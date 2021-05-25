# Microwave Networks

A C# library implementing common tools for working with microwave networks including reading/writing to Touchstone (.snp) files, de/embedding network parameters, etc.

## Currently Supported Features
- Full support for Touchstone file format version 1.0
- Cascading/embedding/de-embedding 2-port networks
- S and T parameters

## Future/In Development Features
- Full support for Touchston file format version 2.0
- Cascading *n-port* networks using the [symmetry extension method](https://ieeexplore.ieee.org/document/4657394)
- Other parameter types (admittance, impedance, etc.)
- Interpolation

# Examples

## Network Data

#### Create a Two-Port S-Parameters Matrix

```c#
// Simple "ideal" 3 dB attenuator at some frequency with an excellent match
ScatteringParametersMatrix atten = new ScatteringParametersMatrix(numPorts: 2)
{
    [1, 1] = NetworkParameter.FromPolarDecibelDegree(-50, 0),
    [1, 2] = NetworkParameter.FromPolarDecibelDegree(-3, 0),
    [2, 1] = NetworkParameter.FromPolarDecibelDegree(-3, 0),
    [2, 2] = NetworkParameter.FromPolarDecibelDegree(-50, 0)
};
```

#### Create Frequency-Dependent Network Data

```c#
NetworkParameter ten_dBLoss = NetworkParameter.FromPolarDecibelDegree(-10, 0);
var exampleData = new NetworkParametersCollection<ScatteringParametersMatrix>(2)
{
    // Create a new 2-port S-parameter matrix, and set S21 to a scalard 10 dB loss.
    [1.0e9] = new ScatteringParametersMatrix(2) { [2, 1] = ten_dBLoss },
    // Alternatively, can index the frequency and specific matrix index all at once
    // i.e. at 2 GHz, set S21 to -10 dB
    [2.0e9, 2, 1] = ten_dBLoss,
    // etc.
    [5.0e9, 2, 1] = ten_dBLoss
};
```

## Touchstone Files

### Simple File IO

#### Reading All Touchstone Data From a File

```c#
INetworkParametersCollection coll = Touchstone.ReadAllData(@"C:\example.s2p");
foreach (FrequencyParametersPair pair in coll)
{
    (double frequency, NetworkParametersMatrix matrix) = pair;
    double insertionLoss = matrix[2, 1].Magnitude_dB;
    Console.WriteLine($"Insertion loss at {frequency} is {insertionLoss} dB");
}
```

#### Writing All Touchstone Data to a File

This example uses the `exampleData` collection shown in [Create Frequency-Dependent Network Data](#create-frequency-dependent-network-data).

```c#
// Create a new Touchstone file with all default settings
Touchstone tsFile = new Touchstone(exampleData);
tsFile.Write(@"C:\example_data.s2p");
```

### Advanced File IO

##### Enumerating/Filtering Touchstone Data with LINQ

Rather than loading all of the data into memory, the following example enumerates each matrix one at a time from the to allow for runtime manipulation without allocating unnecessary memory allocation. 

```c#
string path = @"C:\example.s2p";
var filteredInsertionLoss = from FrequencyParametersPair pair in Touchstone.ReadData(path)
                            where pair.Frequency_Hz > 2.0e9 && pair.Frequency_Hz < 5.0e9
                            let InsertionLoss_dB = pair.Parameters[2, 1].Magnitude_dB
                            select new { pair.Frequency_Hz, InsertionLoss_dB };
```

#### Streaming Touchstone Data to a File

The `TouchstoneWriter` and `TouchstoneReader` classes allow for precise control over Touchstone File IO, including full support for asynchronous file operations. This example uses the `exampleData` collection shown in [Create Frequency-Dependent Network Data](#create-frequency-dependent-network-data).

```c#
TouchstoneWriterSettings settings = new TouchstoneWriterSettings
{
    IncludeColumnNames = true,
    NumericFormatString = "G3"
};

using (TouchstoneWriter writer = TouchstoneWriter.Create(@"C:\example_data.s2p", settings))
{
    writer.Options.FrequencyUnit = FrequencyUnit.GHz;

    writer.WriteCommentLine("This is an example comment at the top of the file.");
    // The header will be written automatically if not called manually as soon as the first call is made to WriteData(). 
    // However, you can manually invoke this if you would like to control where it is placed in relation to other comments.
    writer.WriteHeader();

    foreach (FrequencyParametersPair pair in exampleData)
        writer.WriteData(pair);
}
```

