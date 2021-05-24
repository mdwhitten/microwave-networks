using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Matrices.SymmetryExtension
{
    internal readonly struct NetworkParameterElement : ISymmetryMatrixElement
    {
        private readonly NetworkParameter parameter;

        public NetworkParameterElement(NetworkParameter param) => parameter = param;

        public ISymmetryMatrixElement Inverse() => new NetworkParameterElement(Complex.Reciprocal(parameter));

        public ISymmetryMatrixElement Multiply(ISymmetryMatrixElement other)
        {
            if (other is NetworkParameterElement el)
            {
                return new NetworkParameterElement(parameter * el.parameter);
            }
            else throw new ArgumentException($"Parameter must be of type {typeof(NetworkParameterElement)}", nameof(other));
        }

        public ISymmetryMatrixElement Negate() => new NetworkParameterElement(-parameter);

        public ISymmetryMatrixElement Subtract(ISymmetryMatrixElement other)
        {
            if (other is NetworkParameterElement el)
            {
                return new NetworkParameterElement(parameter - el.parameter);
            }
            else throw new ArgumentException($"Parameter must be of type {typeof(NetworkParameterElement)}", nameof(other));
        }

        public static implicit operator NetworkParameterElement(NetworkParameter param) => new NetworkParameterElement(param);
        public static implicit operator NetworkParameter(NetworkParameterElement element) => element.parameter;
    }
}
