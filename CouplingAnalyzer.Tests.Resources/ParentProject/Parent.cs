using System;
using ChildProject;

namespace ParentProject
{
    public class Parent
    {
        private readonly ChildData childData;

        public Parent(SimpleChild child)
        {
            var _ = new ComplexChild();
        }

        public void Foo(VeryComplexChild veryComplexChild)
        {
            
        }

        public void Handle(IChildInterface i)
        {
            
        }
    }
}
