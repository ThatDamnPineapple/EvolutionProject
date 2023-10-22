//https://rubikscode.net/2022/07/04/implementing-simple-neural-network-in-c/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Core.NeuralNetworks
{
    public interface IInputFunction
    {
        double CalculateInput(List<ISynapse> inputs);
    }

    public interface IActivationFunction
    {
        double CalculateOutput(double input);
    }

    public class WeightedSumFunction : IInputFunction
    {
        public double CalculateInput(List<ISynapse> inputs)
        {
            return inputs.Select(x => x.Weight * x.GetOutput()).Sum();
        }
    }

    public class StepActivationFunction : IActivationFunction
    {
        private double _treshold;

        public StepActivationFunction(double treshold)
        {
            _treshold = treshold;
        }

        public double CalculateOutput(double input)
        {
            return Convert.ToDouble(input > _treshold);
        }
    }

    public class SigmoidActivationFunction : IActivationFunction
    {
        private double _coeficient;

        public SigmoidActivationFunction(double coeficient)
        {
            _coeficient = coeficient;
        }

        public double CalculateOutput(double input)
        {
            return (1 / (1 + Math.Exp(-input * _coeficient)));
        }
    }

    public class RectifiedActivationFuncion : IActivationFunction
    {
        public double CalculateOutput(double input)
        {
            return Math.Max(0, input);
        }
    }

    public interface INeuron
    {
        Guid Id { get; }
        double PreviousPartialDerivate { get; set; }

        List<ISynapse> Inputs { get; set; }
        List<ISynapse> Outputs { get; set; }

        void AddInputNeuron(INeuron inputNeuron);
        void AddOutputNeuron(INeuron inputNeuron);
        double CalculateOutput();

        void AddInputSynapse(double inputValue);
        void PushValueOnInput(double inputValue);
    }

    public class Neuron : INeuron
    {
        private IActivationFunction _activationFunction;
        private IInputFunction _inputFunction;

        /// <summary>
        /// Input connections of the neuron.
        /// </summary>
        public List<ISynapse> Inputs { get; set; }

        /// <summary>
        /// Output connections of the neuron.
        /// </summary>
        public List<ISynapse> Outputs { get; set; }

        public Guid Id { get; private set; }

        /// <summary>
        /// Calculated partial derivate in previous iteration of training process.
        /// </summary>
        public double PreviousPartialDerivate { get; set; }

        public Neuron(IActivationFunction activationFunction, IInputFunction inputFunction)
        {
            Id = Guid.NewGuid();
            Inputs = new List<ISynapse>();
            Outputs = new List<ISynapse>();

            _activationFunction = activationFunction;
            _inputFunction = inputFunction;
        }

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the output neuron of the connection.
        /// </summary>
        /// <param name="inputNeuron">Neuron that will be input neuron of the newly created connection.</param>
        public void AddInputNeuron(INeuron inputNeuron)
        {
            var synapse = new Synapse(inputNeuron, this);
            Inputs.Add(synapse);
            inputNeuron.Outputs.Add(synapse);
        }

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the input neuron of the connection.
        /// </summary>
        /// <param name="outputNeuron">Neuron that will be output neuron of the newly created connection.</param>
        public void AddOutputNeuron(INeuron outputNeuron)
        {
            var synapse = new Synapse(this, outputNeuron);
            Outputs.Add(synapse);
            outputNeuron.Inputs.Add(synapse);
        }

        /// <summary>
        /// Calculate output value of the neuron.
        /// </summary>
        /// <returns>
        /// Output of the neuron.
        /// </returns>
        public double CalculateOutput()
        {
            return _activationFunction.CalculateOutput(_inputFunction.CalculateInput(this.Inputs));
        }

        /// <summary>
        /// Input Layer neurons just receive input values.
        /// For this they need to have connections.
        /// This function adds this kind of connection to the neuron.
        /// </summary>
        /// <param name="inputValue">
        /// Initial value that will be "pushed" as an input to connection.
        /// </param>
        public void AddInputSynapse(double inputValue)
        {
            var inputSynapse = new InputSynapse(this, inputValue);
            Inputs.Add(inputSynapse);
        }

        /// <summary>
        /// Sets new value on the input connections.
        /// </summary>
        /// <param name="inputValue">
        /// New value that will be "pushed" as an input to connection.
        /// </param>
        public void PushValueOnInput(double inputValue)
        {
            ((InputSynapse)Inputs.First()).Output = inputValue;
        }
    }
    public interface ISynapse
    {
        double Weight { get; set; }
        double PreviousWeight { get; set; }
        double GetOutput();

        bool IsFromNeuron(Guid fromNeuronId);
        void UpdateWeight(double learningRate, double delta);
    }

    public class Synapse : ISynapse
    {
        internal INeuron _fromNeuron;
        internal INeuron _toNeuron;

        /// <summary>
        /// Weight of the connection.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Weight that connection had in previous itteration.
        /// Used in training process.
        /// </summary>
        public double PreviousWeight { get; set; }

        public Synapse(INeuron fromNeuraon, INeuron toNeuron, double weight)
        {
            _fromNeuron = fromNeuraon;
            _toNeuron = toNeuron;

            Weight = weight;
            PreviousWeight = 0;
        }

        public Synapse(INeuron fromNeuraon, INeuron toNeuron)
        {
            _fromNeuron = fromNeuraon;
            _toNeuron = toNeuron;

            var tmpRandom = new Random();
            Weight = tmpRandom.NextDouble();
            PreviousWeight = 0;
        }

        /// <summary>
        /// Get output value of the connection.
        /// </summary>
        /// <returns>
        /// Output value of the connection.
        /// </returns>
        public double GetOutput()
        {
            return _fromNeuron.CalculateOutput();
        }

        /// <summary>
        /// Checks if Neuron has a certain number as an input neuron.
        /// </summary>
        /// <param name="fromNeuronId">Neuron Id.</param>
        /// <returns>
        /// True - if the neuron is the input of the connection.
        /// False - if the neuron is not the input of the connection. 
        /// </returns>
        public bool IsFromNeuron(Guid fromNeuronId)
        {
            return _fromNeuron.Id.Equals(fromNeuronId);
        }

        /// <summary>
        /// Update weight.
        /// </summary>
        /// <param name="learningRate">Chossen learning rate.</param>
        /// <param name="delta">Calculated difference for which weight of the connection needs to be modified.</param>
        public void UpdateWeight(double learningRate, double delta)
        {
            PreviousWeight = Weight;
            Weight += learningRate * delta;
        }
    }

    public class InputSynapse : ISynapse
    {
        internal INeuron _toNeuron;

        public double Weight { get; set; }
        public double Output { get; set; }
        public double PreviousWeight { get; set; }

        public InputSynapse(INeuron toNeuron)
        {
            _toNeuron = toNeuron;
            Weight = 1;
        }

        public InputSynapse(INeuron toNeuron, double output)
        {
            _toNeuron = toNeuron;
            Output = output;
            Weight = 1;
            PreviousWeight = 1;
        }

        public double GetOutput()
        {
            return Output;
        }

        public bool IsFromNeuron(Guid fromNeuronId)
        {
            return false;
        }

        public void UpdateWeight(double learningRate, double delta)
        {
            throw new InvalidOperationException("It is not allowed to call this method on Input Connecion");
        }
    }

    public class NeuralLayer
    {
        public List<INeuron> Neurons;

        public NeuralLayer()
        {
            Neurons = new List<INeuron>();
        }

        /// <summary>
        /// Connecting two layers.
        /// </summary>
        public void ConnectLayers(NeuralLayer inputLayer)
        {
            var combos = Neurons.SelectMany(neuron => inputLayer.Neurons, (neuron, input) => new { neuron, input });
            combos.ToList().ForEach(x => x.neuron.AddInputNeuron(x.input));
        }
    }

    /// <summary>
    /// Factory used to create layers.
    /// </summary>
    public class NeuralLayerFactory
    {
        public NeuralLayer CreateNeuralLayer(int numberOfNeurons, IActivationFunction activationFunction, IInputFunction inputFunction)
        {
            var layer = new NeuralLayer();

            for (int i = 0; i < numberOfNeurons; i++)
            {
                var neuron = new Neuron(activationFunction, inputFunction);
                layer.Neurons.Add(neuron);
            }

            return layer;
        }
    }

    public class SimpleNeuralNetwork
    {
        private NeuralLayerFactory _layerFactory;

        internal List<NeuralLayer> _layers;
        internal double _learningRate;
        internal double[][] _expectedResult;

        /// <summary>
        /// Constructor of the Neural Network.
        /// Note:
        /// Initialy input layer with defined number of inputs will be created.
        /// </summary>
        /// <param name="numberOfInputNeurons">
        /// Number of neurons in input layer.
        /// </param>
        public SimpleNeuralNetwork(int numberOfInputNeurons)
        {
            _layers = new List<NeuralLayer>();
            _layerFactory = new NeuralLayerFactory();

            // Create input layer that will collect inputs.
            CreateInputLayer(numberOfInputNeurons);

            _learningRate = 2.95;
        }

        /// <summary>
        /// Add layer to the neural network.
        /// Layer will automatically be added as the output layer to the last layer in the neural network.
        /// </summary>
        public void AddLayer(NeuralLayer newLayer)
        {
            if (_layers.Any())
            {
                var lastLayer = _layers.Last();
                newLayer.ConnectLayers(lastLayer);
            }

            _layers.Add(newLayer);
        }

        /// <summary>
        /// Push input values to the neural network.
        /// </summary>
        public void PushInputValues(double[] inputs)
        {
            _layers.First().Neurons.ForEach(x => x.PushValueOnInput(inputs[_layers.First().Neurons.IndexOf(x)]));
        }

        /// <summary>
        /// Set expected values for the outputs.
        /// </summary>
        public void PushExpectedValues(double[][] expectedOutputs)
        {
            _expectedResult = expectedOutputs;
        }

        /// <summary>
        /// Calculate output of the neural network.
        /// </summary>
        /// <returns></returns>
        public List<double> GetOutput()
        {
            var returnValue = new List<double>();

            _layers.Last().Neurons.ForEach(neuron =>
            {
                returnValue.Add(neuron.CalculateOutput());
            });

            return returnValue;
        }

        /// <summary>
        /// Train neural network.
        /// </summary>
        /// <param name="inputs">Input values.</param>
        /// <param name="numberOfEpochs">Number of epochs.</param>
        public void Train(double[][] inputs, int numberOfEpochs)
        {
            double totalError = 0;

            for (int i = 0; i < numberOfEpochs; i++)
            {
                for (int j = 0; j < inputs.GetLength(0); j++)
                {
                    PushInputValues(inputs[j]);

                    var outputs = new List<double>();

                    // Get outputs.
                    _layers.Last().Neurons.ForEach(x =>
                    {
                        outputs.Add(x.CalculateOutput());
                    });

                    // Calculate error by summing errors on all output neurons.
                    totalError = CalculateTotalError(outputs, j);
                    HandleOutputLayer(j);
                    HandleHiddenLayers();
                }
            }
        }

        /// <summary>
        /// Hellper function that creates input layer of the neural network.
        /// </summary>
        private void CreateInputLayer(int numberOfInputNeurons)
        {
            var inputLayer = _layerFactory.CreateNeuralLayer(numberOfInputNeurons, new RectifiedActivationFuncion(), new WeightedSumFunction());
            inputLayer.Neurons.ForEach(x => x.AddInputSynapse(0));
            this.AddLayer(inputLayer);
        }

        /// <summary>
        /// Hellper function that calculates total error of the neural network.
        /// </summary>
        private double CalculateTotalError(List<double> outputs, int row)
        {
            double totalError = 0;

            outputs.ForEach(output =>
            {
                var error = Math.Pow(output - _expectedResult[row][outputs.IndexOf(output)], 2);
                totalError += error;
            });

            return totalError;
        }

        /// <summary>
        /// Hellper function that runs backpropagation algorithm on the output layer of the network.
        /// </summary>
        /// <param name="row">
        /// Input/Expected output row.
        /// </param>
        private void HandleOutputLayer(int row)
        {
            _layers.Last().Neurons.ForEach(neuron =>
            {
                neuron.Inputs.ForEach(connection =>
                {
                    var output = neuron.CalculateOutput();
                    var netInput = connection.GetOutput();

                    var expectedOutput = _expectedResult[row][_layers.Last().Neurons.IndexOf(neuron)];

                    var nodeDelta = (expectedOutput - output) * output * (1 - output);
                    var delta = -1 * netInput * nodeDelta;

                    connection.UpdateWeight(_learningRate, delta);

                    neuron.PreviousPartialDerivate = nodeDelta;
                });
            });
        }

        /// <summary>
        /// Hellper function that runs backpropagation algorithm on the hidden layer of the network.
        /// </summary>
        /// <param name="row">
        /// Input/Expected output row.
        /// </param>
        private void HandleHiddenLayers()
        {
            for (int k = _layers.Count - 2; k > 0; k--)
            {
                _layers[k].Neurons.ForEach(neuron =>
                {
                    neuron.Inputs.ForEach(connection =>
                    {
                        var output = neuron.CalculateOutput();
                        var netInput = connection.GetOutput();
                        double sumPartial = 0;

                        _layers[k + 1].Neurons
                        .ForEach(outputNeuron =>
                        {
                            outputNeuron.Inputs.Where(i => i.IsFromNeuron(neuron.Id))
                            .ToList()
                            .ForEach(outConnection =>
                            {
                                sumPartial += outConnection.PreviousWeight * outputNeuron.PreviousPartialDerivate;
                            });
                        });

                        var delta = -1 * netInput * sumPartial * output * (1 - output);
                        connection.UpdateWeight(_learningRate, delta);
                    });
                });
            }
        }
    }
}
