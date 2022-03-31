using System;

namespace ChildProject
{
    public class SimpleChild
    {
        public int MyProperty { get; set; }
        private int _myField;

        public SimpleChild()
        {

        }
    }

    public class ComplexChild : SimpleChild
    {
        public void Logic()
        {
            
        }
    }

    public interface IChildInterface
    {
        void ICanDoSomething();
    }

    public struct ChildData
    {
        public int MyProperty { get; set; }

        public void Logic()
        {

        }
    }

    public class VeryComplexChild : ComplexChild
    {
        
    }
}
