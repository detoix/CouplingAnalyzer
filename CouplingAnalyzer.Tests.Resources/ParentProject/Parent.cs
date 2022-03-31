using System;
using ChildProject;
using ChildProject.Extensions;

namespace ParentProject
{
    public class Parent
    {
        private readonly ChildData _childData;

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

    public interface IParent
    {
        void Test(OtherChild any);
    }

    public struct ParentData
    {
        void Foo()
        {
            new object().Anything();
        }
    }
}
