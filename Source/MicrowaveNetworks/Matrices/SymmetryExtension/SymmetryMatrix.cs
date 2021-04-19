using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Complex;
using System.Numerics;

namespace MicrowaveNetworks.Matrices.SymmetryExtension
{
    internal struct SymmetryMatrix<T>
    {

        //public SymmetryMatrix(int numPorts) => NumberOfPorts = numPorts;

        public int NumberOfPorts { get; set; }

        public ElementAbstraction<T> OneOne { get; set; }
        public ElementAbstraction<T> OneTwo { get; set; }
        public ElementAbstraction<T> TwoOne { get; set; }
        public ElementAbstraction<T> TwoTwo { get; set; }

        public ElementAbstraction<T> this[int row, int column]
        {
            get
            {
                switch (row)
                {
                    case 1 when column == 1: return OneOne;
                    case 1 when column == 2: return OneTwo;
                    case 2 when column == 1: return TwoOne;
                    case 2 when column == 2: return TwoTwo;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (row)
                {
                    case 1 when column == 1: 
                        OneOne = value;
                        break;
                    case 1 when column == 2:
                        OneTwo = value;
                        break;
                    case 2 when column == 1:
                        TwoOne = value;
                        break;
                    case 2 when column == 2:
                        TwoTwo = value;
                        break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }


        public static SymmetryMatrix<NetworkParameter> From2PortData(NetworkParametersMatrix m)
        {
            return new SymmetryMatrix<NetworkParameter>
            {
                NumberOfPorts = 2,

                OneOne = m[1, 1],
                OneTwo = m[1, 2],
                TwoOne = m[2, 1],
                TwoTwo = m[2, 2]
            };
        }
    }
}
