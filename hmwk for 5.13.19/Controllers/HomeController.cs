using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using hmwk_for_5._13._19.Models;
using Microsoft.Extensions.Configuration;
using Data;

namespace hmwk_for_5._13._19.Controllers
{
    public class HomeController : Controller
    {
        private string _conn;
        public HomeController(IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("ConStr");
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var repo = new JokesRepository(_conn);
                User user = repo.GetUserByEmail(User.Identity.Name);
                Joke joke = repo.GenerateRandomJoke();
                var vm = new RandomJokeViewModel
                {
                    Joke = joke,
                    User = user,
                    Liked = repo.DidUserLike(user.Id, joke.Id),
                    Disliked = repo.DidUserDislike(user.Id, joke.Id)
                };
                return View(vm);
            }
            else
            {
                return Redirect("/account/login");
            }

        }

        [HttpPost]
        public void Like(int userId, int jokeId)
        {
            var repo = new JokesRepository(_conn);
            repo.LikeJoke(userId, jokeId);
            //return Redirect("/home/jokes");
        }

        [HttpPost]
        public void Dislike(int userId, int jokeId)
        {
            var repo = new JokesRepository(_conn);
            repo.DislikeJoke(userId, jokeId);
            //return Redirect("/home/jokes");
        }

        public IActionResult Jokes()
        {
            var repo = new JokesRepository(_conn);
            var jokesVm = new List<JokeViewModel>();
            var jokes = repo.GetJokes();
            foreach (Joke j in jokes)
            {
                jokesVm.Add(new JokeViewModel
                {
                    Joke = j,
                    Likes = repo.GetLikesByJokeId(j.Id),
                    Dislikes = repo.GetDislikesByJokeId(j.Id),
                });
            }
            return View(jokesVm);
        }

        public IActionResult StillLike(UserLikedJokes like)
        {
            var repo = new JokesRepository(_conn);
            var liked = repo.CanStillLike(like);
            return Json(new { liked = liked });
        }
    }
}
