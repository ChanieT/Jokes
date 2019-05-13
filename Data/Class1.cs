using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Data
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public List<UserLikedJokes> UserLikedJokes { get; set; }
    }

    public class Joke
    {
        public int Id { get; set; }
        [JsonProperty("id")]
        public int JokeId { get; set; }
        public string Setup { get; set; }
        public string Punchline { get; set; }
        //public int Likes { get; set; }

        public List<UserLikedJokes> UserLikedJokes { get; set; }
    }

    public class UserLikedJokes
    {
        public int UserId { get; set; }
        public int JokeId { get; set; }
        public DateTime Time { get; set; }
        public bool Liked { get; set; }

        public Joke Joke { get; set; }
        public User User { get; set; }
    }

    public class JokesContext : DbContext
    {
        private string _conn;
        public JokesContext(string conn)
        {
            _conn = conn;
        }

        public DbSet<Joke> Jokes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserLikedJokes> UserLikedJokes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_conn);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<UserLikedJokes>()
                .HasKey(ulj => new { ulj.UserId, ulj.JokeId });


            modelBuilder.Entity<UserLikedJokes>()
                .HasOne(ulj => ulj.User)
                .WithMany(u => u.UserLikedJokes)
                .HasForeignKey(u => u.UserId);

            modelBuilder.Entity<UserLikedJokes>()
                .HasOne(ulj => ulj.Joke)
                .WithMany(j => j.UserLikedJokes)
                .HasForeignKey(ulj => ulj.JokeId);
        }
    }

    public class JokesContextFactory : IDesignTimeDbContextFactory<JokesContext>
    {
        public JokesContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), $"..{Path.DirectorySeparatorChar}hmwk for 5.13.19"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true).Build();

            return new JokesContext(config.GetConnectionString("ConStr"));
        }
    }

    public class JokesRepository
    {
        private string _conn;
        public JokesRepository(string conn)
        {
            _conn = conn;
        }

        public void AddUser(User u)
        {
            using (var context = new JokesContext(_conn))
            {
                var user = new User
                {
                    Name = u.Name,
                    Email = u.Email,
                    Password = HashPassword(u.Password)
                };
                context.Users.Add(user);
                context.SaveChanges();
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Match(string input, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(input, passwordHash);
        }

        public User GetUserByEmail(string email)
        {
            using (var context = new JokesContext(_conn))
            {
                return context.Users.FirstOrDefault(u => u.Email == email);
            }
        }

        public Joke GenerateRandomJoke()
        {
            using (var context = new JokesContext(_conn))
            {
                var client = new HttpClient();
                string url = "https://official-joke-api.appspot.com/jokes/programming/random";
                string json = client.GetStringAsync(url).Result;
                var result = JsonConvert.DeserializeObject<IEnumerable<Joke>>(json).First();
                context.Jokes.Add(result);
                context.SaveChanges();
                return result;
            }
        }

        public void LikeJoke(int userId, int jokeId)
        {
            using (var context = new JokesContext(_conn))
            {
                if (context.UserLikedJokes.Any(l => l.UserId == userId && l.JokeId == jokeId))
                {
                    UpdateLike(userId, jokeId, true);
                }
                else
                {
                    context.UserLikedJokes.Add(new UserLikedJokes
                    {
                        UserId = userId,
                        JokeId = jokeId,
                        //Liked = true,
                        Time = DateTime.Now
                    });
                    //UpdateLike(userId, jokeId, true);
                    context.SaveChanges();
                    UpdateLike(userId, jokeId, true);
                }
            }
        }

        public void DislikeJoke(int userId, int jokeId)
        {
            using (var context = new JokesContext(_conn))
            {
                if (context.UserLikedJokes.Any(l => l.UserId == userId && l.JokeId == jokeId))
                {
                    UpdateLike(userId, jokeId, false);
                }
                else
                {
                    context.UserLikedJokes.Add(new UserLikedJokes
                    {
                        UserId = userId,
                        JokeId = jokeId,
                        //Liked = false,
                        Time = DateTime.Now
                    });
                    //UpdateLike(userId, jokeId, false);
                    context.SaveChanges();
                    UpdateLike(userId, jokeId, false);
                }
            }
        }

        public void UpdateLike(int userId, int jokeId, bool like)
        {
            using (var context = new JokesContext(_conn))
            {
                context.Database.ExecuteSqlCommand(
                "UPDATE UserLikedJokes SET Liked=@liked WHERE JokeId=@jokeId AND UserId=@userId",
                new SqlParameter("@liked", like),
                new SqlParameter("@jokeId", jokeId),
                new SqlParameter("@userId", userId));
            }
        }

        public IEnumerable<Joke> GetJokes()
        {
            using (var context = new JokesContext(_conn))
            {
                return context.Jokes.Include(j => j.UserLikedJokes).ToList();
            }
        }

        public bool DidUserLike(int userId, int jokeId)
        {
            using (var context = new JokesContext(_conn))
            {
                return context.UserLikedJokes.Any(l => l.UserId == userId
                && l.JokeId == jokeId
                && l.Liked == true);
            }
        }

        public bool DidUserDislike(int userId, int jokeId)
        {
            using (var context = new JokesContext(_conn))
            {
                return context.UserLikedJokes.Any(l => l.UserId == userId
                && l.JokeId == jokeId
                && l.Liked == false);
            }
        }

        public int GetLikesByJokeId(int id)
        {
            using (var context = new JokesContext(_conn))
            {
                var likes = context.Jokes.Include(j => j.UserLikedJokes).FirstOrDefault(j => j.Id == id).UserLikedJokes.Where(ulj => ulj.Liked == true);
                if (likes == null)
                {
                    return 0;
                }
                else
                {
                    return likes.Count();
                }
            }
        }

        public int GetDislikesByJokeId(int id)
        {
            using (var context = new JokesContext(_conn))
            {
                var dislikes = context.Jokes.Include(j => j.UserLikedJokes).FirstOrDefault(j => j.Id == id).UserLikedJokes.Where(ulj => ulj.Liked == false);
                if (dislikes == null)
                {
                    return 0;
                }
                else
                {
                    return dislikes.Count();
                }
            }
        }

        public bool CanStillLike(UserLikedJokes ulj)
        {
            using (var context = new JokesContext(_conn))
            {
                var like = context.UserLikedJokes.FirstOrDefault(l => l.UserId == ulj.UserId
                  && l.JokeId == ulj.JokeId);
                if (like != null)
                {
                    return like.Time.AddMinutes(5) > DateTime.Now;
                }
                else
                {
                    return true;
                }
            }
        }
    }

}
