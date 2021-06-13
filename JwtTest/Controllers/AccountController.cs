using JwtTest.EF;
using JwtTest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtTest.Helpers;
using Microsoft.AspNetCore.Identity;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace JwtTest.Controllers
{
	public class AccountController : BaseController
	{


		public AccountController(JwtContext context, IOptions<AuthOptions> options, IHostEnvironment hostEnvironment)
		{
			this.context = context;
			this.options = options;
			this.hostEnvironment = hostEnvironment;
		}

		[HttpPost("/token")]
		public IActionResult Token(string username, string password)
		{
			var identity = GetIdentity(username, password);
			if (identity == null)
			{
				return BadRequest(new { errorText = "Invalid username or password." });
			}

			var now = DateTime.UtcNow;
			// создаем JWT-токен
			var jwt = new JwtSecurityToken(
					issuer: options.Value.Issuer,
					audience: options.Value.Audience,
					notBefore: now,
					claims: identity.Claims,
					expires: now.Add(TimeSpan.FromMinutes(options.Value.Lifetime)),
					signingCredentials: new SigningCredentials(options.Value.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
			var response = new
			{
				access_token = encodedJwt,
				username = identity.Name
			};
			return Json(response);
		}

		private ClaimsIdentity GetIdentity(string username, string password)
		{
			Person person = context.People.SingleOrDefault(x => x.Login == username);
			if (person != null && Argon2.Verify(person.PasswordHash, password))
			{
				var claims = new List<Claim>
				{
					new Claim(ClaimsIdentity.DefaultNameClaimType, person.Login),
					new Claim(ClaimsIdentity.DefaultRoleClaimType, Enum.GetName(person.Role))
				};
				ClaimsIdentity claimsIdentity =
				new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
					ClaimsIdentity.DefaultRoleClaimType);
				return claimsIdentity;
			}
			// если пользователя не найдено
			return null;
		}

		private async Task<bool> RegisterOrder(string description, string content, DateTime date, Person customer)
		{
			ArtOrder order = new ArtOrder()
			{
				Description = description,
				Content = content,
				DeadLine = date,
				Customer = customer,
				Accepted = false,
				Confirm = false
			};
			await context.Orders.AddAsync(order);
			await context.SaveChangesAsync();
			return true;
		}


		private async Task<bool> RegisterUser(string username, string password, string email, UserRole role, IFormFile file)
		{
			if (context.People.Any(p => p.Login == username))
				return false;
			string randomFile = null;
			if (file != null)
			{
				randomFile = $"{Path.GetRandomFileName()}.{Path.GetExtension(file.FileName)}";

			}
			Person person = new Person()
			{
				Login = username,
				PasswordHash = Argon2.Hash(password),
				ContactEmail = email,
				Role = role,
				Avatar = randomFile,
				IsActive = true
			};
			await context.People.AddAsync(person);
			await context.SaveChangesAsync();
			if (file != null)
			{
				person = context.Entry(person).Entity;
				string userPath = Path.Combine(ImageFolder, person.Id.ToString());
				if (!Directory.Exists(userPath))
					Directory.CreateDirectory(userPath);
				await file.WriteToFile(Path.Combine(userPath, randomFile));
			}
			return true;
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpGet]
		public IActionResult MainPage()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterModel model)
		{
			if (!ModelState.IsValid )
				return View(model);
			if (await RegisterUser(model.Username, model.Password, model.Email, UserRole.User, model.Avatar))
				return Redirect("/Home/Index");
			else
			{
				ModelState.AddModelError("Username", "Данное имя уже используется или пароли не совпадают");
				return (View(model));
			}
		}

		//

		[Authorize(Roles = "Admin")]
		private async Task<bool> AddFileEx(AddFileToOrderModel model, IFormFile file)
		{
			string randomFile = null;
			if (file != null)
			{
				randomFile = $"{Path.GetRandomFileName()}.{Path.GetExtension(file.FileName)}";

			}
			ArtOrder order = context.Orders.Find(model.Id);
			if (order != null)
			{
				order.Confirm = true;
				order.Accepted = true;
				order.PathToFile = randomFile;
				order.OrderStatus = ArtOrder.Status.done;
				await context.SaveChangesAsync();
			}
			await context.SaveChangesAsync();
			if (file != null)
			{
				order = context.Entry(order).Entity;
				string orderPath = Path.Combine(Path.Combine(ImageFolder, order.Customer.Id.ToString()), order.Id.ToString());
				if (!Directory.Exists(orderPath))
					Directory.CreateDirectory(orderPath);
				await file.WriteToFile(Path.Combine(orderPath, randomFile));
			}
			return true;
		}
		[Authorize(Roles = "Admin")]
		public IActionResult AddFileToOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			return View(ConverterHelper.ToAddFileToOrderModel(order));
		}
		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> AddFile(AddFileToOrderModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			if (await AddFileEx(model, model.File))
				return Redirect("Order");
			else
			{
				return (View(model));
			}
		}

		//

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			Person person = context.People.SingleOrDefault(usr => usr.Login == model.Username);
			if (person == null || !Argon2.Verify(person.PasswordHash, model.Password))
			{
				ModelState.AddModelError("Username", "Неверное имя пользователя или пароль");
				return View(model);
			}
			else if (!person.IsActive)
			{
				ModelState.AddModelError("Username", "Этот пользователь удален администратором");
				return View(model);
			}
			await Authenticate(person.Login, person.Role);
			return Redirect("/Home/Index");
		}

		[Authorize]
		public async Task<IActionResult> LogOff()
		{
			await Logout();
			return Redirect("/Home/Index");
		}

		[Authorize(Roles = "Admin")]
		public IActionResult CreateUser()
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreateUser(UserModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			if (await RegisterUser(model.Username, model.Password, model.Email, model.Role, model.Avatar))
				return Redirect("/Home/Index");
			else
			{
				ModelState.AddModelError("Username", "Данное имя уже используется");
				return (View(model));
			}
		}

		[Authorize(Roles = "Admin")]
		public IActionResult ListUsers()
		{
			return View(context.People);
		}

		[Authorize(Roles = "Admin")]
		public IActionResult EditUser(int id)
		{
			Person person = context.People.Find(id);
			return View(person.ToEditUserModel());
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> EditUser(EditUserModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			Person person = context.People.Find(model.Id);
			if (person != null)
			{
				bool taken = person.Login != model.Username && context.People.Any(p => p.Login == model.Username);
				if (taken)
				{
					ModelState.AddModelError("Username", "Данное имя уже занято");
					return (View(model));
				}
				if (model.Avatar != null)
				{
					string userDir = Path.Combine(ImageFolder, person.Id.ToString());
					if (person.Avatar != null)
						System.IO.File.Delete(Path.Combine(userDir, person.Avatar));
					else if (!Directory.Exists(userDir))
						Directory.CreateDirectory(userDir);
					person.Avatar = $"{Path.GetRandomFileName()}.{Path.GetExtension(model.Avatar.FileName)}";
					await model.Avatar.WriteToFile(Path.Combine(userDir, person.Avatar));
				}
				person.Login = model.Username;
				if (!string.IsNullOrEmpty(model.NewPassword))
					person.PasswordHash = Argon2.Hash(model.NewPassword);
				person.Role = model.Role;
				await context.SaveChangesAsync();
				return Redirect("/Home/Index");
			}
			else
			{
				ModelState.AddModelError("", "Неверный ID");
				return (View(model));
			}
		}

		[Authorize(Roles = "Admin")]
		public IActionResult UserDetails(int id)
		{
			Person person = context.People.Find(id);
			return View(person.ToUserModel());
		}


		[Authorize(Roles = "Admin")]
		public IActionResult DeleteUser(int id)
		{
			Person person = context.People.Find(id);
			return View(person.ToUserModel());
		}


		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DelUser(int id)
		{
			Person person = context.People.Find(id);
			if (person != null)
			{
				person.IsActive = false;				
				await context.SaveChangesAsync();
			}
			return Redirect("ListUsers");
		}
		[Authorize(Roles = "Admin")]
		public IActionResult Order()
		{
			return View(context.Orders);
		}

		[Authorize(Roles = "User")]
		public IActionResult MyOrder()
		{
			UserModel usr = CurrentUser.ToUserModel();
			List<ArtOrder> res = new List<ArtOrder>();

			foreach (ArtOrder order in context.Orders)
			{
				try
				{
					if (order.Customer.Id == usr.Id)
					{
						res.Add(order);
					}
				}
				catch
				{

				}
			}

			return View(res);
		}


		public IActionResult CreateOrder()
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "User")]
		public async Task<IActionResult> CreateOrder(OrderModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			if (await RegisterOrder(model.Description, model.Content, model.DeadLine, CurrentUser))
				return Redirect("/Home/Index");
			else
			{
				//ModelState.AddModelError("Description", "Заказ с таким описанием уже существует");
				return (View(model));
			}
		}
		[Authorize]
		public IActionResult DeleteOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			return View(ConverterHelper.ToDeleteOrderModel(order));
		}
		[Authorize]
		public async Task<IActionResult> DelOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			if (order != null)
			{
				order.OrderStatus = ArtOrder.Status.rejected;
				await context.SaveChangesAsync();
			}
			if (CurrentUser.Role == UserRole.Admin)
			{
				return Redirect("Order");
			}
			else
			{
				return Redirect("MyOrder");
			}
		}


		[Authorize(Roles = "User")]
		public IActionResult EditOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			return View(ConverterHelper.ToEditOrderModel(order));
		}

		[HttpPost]
		[Authorize(Roles = "User")]
		public async Task<IActionResult> EdOrder(EditOrderModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			ArtOrder order = context.Orders.Find(model.Id);
			if (order != null)
			{
				order.Description = model.Description;
				order.Content = model.Content;
				order.OrderStatus = model.OrderStatus;
				order.DeadLine = model.DeadLine;
				order.Price = model.Price;
				order.Confirm = false;
				order.Accepted = false;
				await context.SaveChangesAsync();
				return Redirect("MyOrder");
			}
			else
			{
				ModelState.AddModelError("", "Неверный ID");
				return (View(model));
			}
		}

		[Authorize(Roles = "User")]
		public IActionResult AcceptOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			return View(ConverterHelper.ToAcceptOrderModel(order));
		}

		[Authorize(Roles = "User")]
		public async Task<IActionResult> AccOrder(AcceptOrderModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			ArtOrder order = context.Orders.Find(model.Id);
			if (order != null)
			{
				order.OrderStatus = ArtOrder.Status.inProgress;
				order.Accepted = true;
				await context.SaveChangesAsync();
				return Redirect("MyOrder");
			}
			else
			{
				ModelState.AddModelError("", "Неверный ID");
				return (View(model));
			}
		}

		[Authorize(Roles = "Admin")]
		public IActionResult ConfirmOrder(int id)
		{
			ArtOrder order = context.Orders.Find(id);
			return View(ConverterHelper.ToConfirmOrderModel(order));
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> ConfOrder(ConfirmOrderModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			ArtOrder order = context.Orders.Find(model.Id);
			if (order != null)
			{
				order.Description = model.Description;
				order.Content = model.Content;
				order.OrderStatus = model.OrderStatus;
				order.DeadLine = model.DeadLine;
				order.Price = model.Price;
				order.Confirm = true;
				await context.SaveChangesAsync();
				return Redirect("Order");
			}
			else
			{
				ModelState.AddModelError("", "Неверный ID");
				return (View(model));
			}
		}

		[Authorize]
		public IActionResult Userpage()
		{
			UserModel usr = CurrentUser.ToUserModel();
			return View(usr);
		}


		private string GetContentType(string filename)
		{
			string contentType;
			new FileExtensionContentTypeProvider().TryGetContentType(filename, out contentType);
			return contentType ?? "application/octet-stream";
		}

		[Authorize]
		public async Task<IActionResult> Avatar(string username)
		{
			Person person = context.People.FirstOrDefault(p => p.Login == username);

			string filePath;
			if (person == null || person.Avatar == null)
				filePath = Path.Combine(hostEnvironment.ContentRootPath, "DefaultImages", "no_ava.png");
			else
				filePath = Path.Combine(ImageFolder, person.Id.ToString(), person.Avatar);
			string contentType = GetContentType(filePath);
			byte[] imgBytes = await System.IO.File.ReadAllBytesAsync(filePath);
			return File(imgBytes, contentType);
		}


		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> ListBlog()
		{
			UserModel usr = CurrentUser.ToUserModel();
			Person usrcont = context.People.FirstOrDefault(p => p.Login == usr.Username);
			List<Blog> blog = await context.Blogs.ToListAsync();
			blog.Reverse();
			return View(blog);
		}


		public async Task<IActionResult> AllListBlog()
		{
			List<Blog> blog = await context.Blogs.ToListAsync();
			blog.Reverse();
			return View(blog);
		}

		[Authorize]
		public async Task<IActionResult> DeleteBlog(int id)
		{
			ClaimsIdentity cookieClaims = User.Identities.FirstOrDefault(cc => cc.AuthenticationType == "ApplicationCookie");
			bool authenticated = cookieClaims != null && cookieClaims.IsAuthenticated;
			if (authenticated)
			{
				Blog blog = context.Blogs.Find(id);
				if (blog != null)
				{
					context.Blogs.Remove(blog);
					await context.SaveChangesAsync();
				}
				Claim roleClaim = cookieClaims.Claims.FirstOrDefault(cc => cc.Type == cookieClaims.RoleClaimType);
				if (roleClaim.Value == "Admin")
				{
					return Redirect("/Account/AllListBlog");
				}
				else if (roleClaim.Value == "User")
				{
					return Redirect("/Account/ListBlog");
				}
			}
			return Redirect("/Account/AllListBlog");
		}


		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult CreateBlog()
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> CreateBlog(BlogModel model)
		{
			if (!ModelState.IsValid)
				return View(model);
			if (await RegisterBlog(model.Title,




									model.Comment,
									model.Picture))
				return Redirect("/Account/ListBlog");
			else
			{
				ModelState.AddModelError("Blog", "Произошла ошибка, пожалуйста повторите");
				return (View(model));
			}
		}

		private async Task<bool> RegisterBlog(string bird,
												string comment,
												IFormFile file)
		{
			UserModel usr = CurrentUser.ToUserModel();
			Person usrcont = context.People.FirstOrDefault(p => p.Login == usr.Username);

			string randomFile = null;
			if (file != null)
			{
				randomFile = $"{Path.GetRandomFileName()}.{Path.GetExtension(file.FileName)}";
			}
			Blog blog = new Blog()
			{
				Title = bird,

				Time = DateTime.Now,

				Comment = comment,
				Author = usrcont,
				Picture = randomFile
			};

			await context.Blogs.AddAsync(blog);
			await context.SaveChangesAsync();
			if (file != null)
			{
				blog = context.Entry(blog).Entity;
				string userPath = Path.Combine(ImageFolder, blog.Id.ToString());
				if (!Directory.Exists(userPath))
					Directory.CreateDirectory(userPath);
				await file.WriteToFile(Path.Combine(userPath, randomFile));
			}
			return true;
		}


		public async Task<IActionResult> BlogPicture(string username)
		{
			Blog blog = context.Blogs.FirstOrDefault(p => p.Title == username);

			string filePath;
			if (blog == null || blog.Picture == null)
				filePath = Path.Combine(hostEnvironment.ContentRootPath, "DefaultImages", "birdDefault.png");
			else
				filePath = Path.Combine(ImageFolder, blog.Id.ToString(), blog.Picture);
			string contentType = GetContentType(filePath);
			byte[] imgBytes = await System.IO.File.ReadAllBytesAsync(filePath);
			return File(imgBytes, contentType);
		}

		public async Task<IActionResult> OrderPicture(int Id)
		{
			ArtOrder order = context.Orders.FirstOrDefault(p => p.Id == Id);

			string filePath;
			if (order == null || order.PathToFile == null)
				filePath = Path.Combine(hostEnvironment.ContentRootPath, "DefaultImages", "birdDefault.png");
			else
				filePath = Path.Combine(ImageFolder, order.Customer.Id.ToString(), order.Id.ToString(), order.PathToFile);
			string contentType = GetContentType(filePath);
			byte[] imgBytes = await System.IO.File.ReadAllBytesAsync(filePath);
			return File(imgBytes, contentType);
		}

	}
}
