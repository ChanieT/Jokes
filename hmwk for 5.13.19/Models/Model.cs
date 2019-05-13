using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;

namespace hmwk_for_5._13._19.Models
{
    public class RandomJokeViewModel
    {
        public Joke Joke { get; set; }
        public User User { get; set; }
        public bool Liked { get; set; }
        public bool Disliked { get; set; }
    }

    public class JokeViewModel
    {
        public Joke Joke { get; set; }
        public int Likes { get; set; }
        public int Dislikes { get; set; }
    }
}
