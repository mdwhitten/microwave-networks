using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra.Complex;

namespace MicrowaveNetworks.Matrices.SymmetryExtension
{
    public struct ElementAbstraction<T>
    {
        T element;
        public ElementAbstraction(T value) { element = value; }
        public T Element => element;
        public ElementAbstraction<T> Add(ElementAbstraction<T> other)
        {
            switch (element)
            {
                case NetworkParameter c when other.element is NetworkParameter o:
                    return new ElementAbstraction<T>((T)(object)(c + o));
                case DenseMatrix a when other.element is DenseMatrix b:
                    return new ElementAbstraction<T>((T)(object)(a + b));
                default:
                    throw new InvalidOperationException();
            }
        }
        public ElementAbstraction<T> Subtract(ElementAbstraction<T> other)
        {
            switch (element)
            {
                case NetworkParameter c when other.element is NetworkParameter o:
                    return new ElementAbstraction<T>((T)(object)(c - o));
                case DenseMatrix a when other.element is DenseMatrix b:
                    return new ElementAbstraction<T>((T)(object)(a - b));
                default:
                    throw new InvalidOperationException();
            }
        }
        public ElementAbstraction<T> Multiply(ElementAbstraction<T> other)
        {
            switch (element)
            {
                case NetworkParameter c when other.element is NetworkParameter o:
                    return new ElementAbstraction<T>((T)(object)(c * o));
                case DenseMatrix a when other.element is DenseMatrix b:
                    return new ElementAbstraction<T>((T)(object)(a * b));
                default:
                    throw new InvalidOperationException();
            }
        }
        public ElementAbstraction<T> Negate()
        {
            switch (element)
            {
                case NetworkParameter c:
                    return new ElementAbstraction<T>((T)(object)-c);
                case DenseMatrix a:
                    return new ElementAbstraction<T>((T)(object)-a);
                default:
                    throw new InvalidOperationException();
            }
        }
        public ElementAbstraction<T> Inverse()
        {
            switch (element)
            {
                case NetworkParameter c:
                    return new ElementAbstraction<T>((T)(object)(NetworkParameter)Complex.Reciprocal(c));
                case DenseMatrix a:
                    return new ElementAbstraction<T>((T)(object)a.Inverse());
                default:
                    throw new InvalidOperationException();
            }
        }

        public static implicit operator T(ElementAbstraction<T> element) => element;
        public static implicit operator ElementAbstraction<T>(T element) => new ElementAbstraction<T>(element);

        public static ElementAbstraction<T> operator +(ElementAbstraction<T> first, ElementAbstraction<T> second) => first.Add(second);
        public static ElementAbstraction<T> operator -(ElementAbstraction<T> first, ElementAbstraction<T> second) => first.Subtract(second);
        public static ElementAbstraction<T> operator -(ElementAbstraction<T> first) => first.Negate();
        public static ElementAbstraction<T> operator *(ElementAbstraction<T> first, ElementAbstraction<T> second) => first.Multiply(second);

    }
}
