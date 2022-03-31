using System;

namespace ChildProject.Extensions
{
    public class OtherChild : IChildInterface
    {
        public void ICanDoSomething()
        {

        }
    }

    public static class Extensions
    {
        public static void Anything(this object anything)
        {
            
        }
    }
}