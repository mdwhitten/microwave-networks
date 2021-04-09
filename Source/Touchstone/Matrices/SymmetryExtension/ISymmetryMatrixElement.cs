using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Matrices.SymmetryExtension
{
    internal interface ISymmetryMatrixElement
    {
        ISymmetryMatrixElement Inverse();

        ISymmetryMatrixElement Multiply(ISymmetryMatrixElement other);
        ISymmetryMatrixElement Subtract(ISymmetryMatrixElement other);
        ISymmetryMatrixElement Negate();
    }

    internal struct SymmetryMatrixElement : ISymmetryMatrixElement
    {
        ISymmetryMatrixElement el;

        public SymmetryMatrixElement(ISymmetryMatrixElement element) => el = element;

        public SymmetryMatrixElement Inverse() => new SymmetryMatrixElement(el.Inverse());

        ISymmetryMatrixElement ISymmetryMatrixElement.Inverse()
        {
            throw new NotImplementedException();
        }

        ISymmetryMatrixElement ISymmetryMatrixElement.Multiply(ISymmetryMatrixElement other)
        {
            throw new NotImplementedException();
        }

        ISymmetryMatrixElement ISymmetryMatrixElement.Subtract(ISymmetryMatrixElement other)
        {
            throw new NotImplementedException();
        }

        ISymmetryMatrixElement ISymmetryMatrixElement.Negate()
        {
            throw new NotImplementedException();
        }

        public static implicit operator SymmetryMatrixElement(NetworkParameterElement d) => new SymmetryMatrixElement(d);
        //public static explicit operator NetworkParameterElement(SymmetryMatrixElement el) => (NetworkParameterElement)el;

        public static SymmetryMatrixElement operator *(SymmetryMatrixElement left, SymmetryMatrixElement right) => new SymmetryMatrixElement(left.el.Multiply(right.el));
        public static SymmetryMatrixElement operator -(SymmetryMatrixElement left, SymmetryMatrixElement right) => new SymmetryMatrixElement(left.el.Subtract(right.el));
        public static SymmetryMatrixElement operator -(SymmetryMatrixElement element) => new SymmetryMatrixElement(element.el.Negate());
    }
}
