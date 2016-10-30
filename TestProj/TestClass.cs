using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProj
{
    public class TestClass
    {
        private readonly string _unforgiven;

        public TestClass()
        {
            _unforgiven = "Never free " +
                          "Never me " +
                          "So I dub thee Unforgiven";
        }

        public IEnumerable<string> Counter()
        {
            foreach (var character in _unforgiven.ToCharArray())
            {
                yield return character.ToString();
            }
        }
    }
}
