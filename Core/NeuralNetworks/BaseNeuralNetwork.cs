using EvoSim.Core.NeuralNetworks.NEAT;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvoSim.Helpers;

namespace EvoSim.Core.NeuralNetworks
{
    public class BaseNeuralNetwork : INeuralNetwork
    {
        public NetLayer Inputs;
        public NetLayer Outputs;

        public List<NetLayer> HiddenLayers = new List<NetLayer>();
        public int Size => HiddenLayers.Count + 2;

        public float[] Response => Outputs.GetValues();

        public BaseNeuralNetwork(int inputSize)
        {
            Inputs = new NetLayer(inputSize, null);
        }

        public NetLayer GetLayer(int index)
        {
            if (index == 0) return Inputs;
            else if (index == Size - 1) return Outputs;
            else return HiddenLayers[index - 1];
        }

        public void Draw(SpriteBatch sb, Vector2 position)
        {
            
        }

        public void Compute(float[] inputs)
        {
            Inputs.Map(inputs);

            for (int i = 1; i < Size; i++)
            {
                GetLayer(i).ComputeLayer(GetLayer(i - 1));
            }
        }

        public BaseNeuralNetwork AddLayer<T>(int size) where T : ActivationFunction, new()
        {
            HiddenLayers.Add(new NetLayer(size, new T()));
            return this;
        }

        public BaseNeuralNetwork AddLayer(int size, ActivationFunction function)
        {
            HiddenLayers.Add(new NetLayer(size, function));
            return this;
        }

        public BaseNeuralNetwork SetOutput<T>(int size) where T : ActivationFunction, new()
        {
            Outputs = new NetLayer(size, new T());
            return this;
        }

        public BaseNeuralNetwork SetOutput(int size, ActivationFunction function)
        {
            Outputs = new NetLayer(size, function);
            return this;
        }

        public BaseNeuralNetwork GenerateWeights(Func<float> weightInitialisationFunction = null)
        {
            for (int i = 0; i < Size - 1; i++)
            {
                GetLayer(i).GenerateWeights(GetLayer(i + 1).Size, weightInitialisationFunction);
            }
            return this;
        }

        public IDna Combine(IDna combinee, float mutationRate)
        {
            BaseNeuralNetwork networkClone = new BaseNeuralNetwork(Inputs.Size);
            for (int i = 0; i < HiddenLayers.Count; i++) networkClone.AddLayer(HiddenLayers[i].Size, HiddenLayers[i].activationFunction);
            networkClone.SetOutput(Outputs.Size, Outputs.activationFunction);

            networkClone.GenerateWeights(null);
            if (combinee is BaseNeuralNetwork combineeNetwork)
            {
                for (int i = 0; i < Size - 1; i++)
                {
                    NetLayer layer = GetLayer(i);

                    NetLayer cloneLayer = networkClone.GetLayer(i);
                    NetLayer combineeLayer = combineeNetwork.GetLayer(i);
                    for (int j = 0; j < layer.nodes.Count; j++)
                    {
                        float biasConfirmation = Main.random.NextFloat();

                        if (biasConfirmation > 0.5f) cloneLayer.nodes[j].bias = combineeLayer.nodes[j].bias;
                        else cloneLayer.nodes[j].bias = layer.nodes[j].bias;

                        if (Main.random.NextFloat(1) < mutationRate)
                        {
                            cloneLayer.nodes[j].bias = Main.random.NextFloat(-1f, 1f);
                        }

                        for (int k = 0; k < layer.nodes[j].weights.Length; k++)
                        {
                            float weightConfirmation = Main.random.NextFloat();

                            if (weightConfirmation > 0.5f) cloneLayer.nodes[j].weights[k] = combineeLayer.nodes[j].weights[k];
                            else cloneLayer.nodes[j].weights[k] = layer.nodes[j].weights[k];

                            if (Main.random.NextFloat(1) < mutationRate)
                            {
                                cloneLayer.nodes[j].weights[k] = Main.random.NextFloat(-1f, 1f);
                            }
                        }
                    }
                }

                return networkClone;
            }
            return combinee;
        }
    }
}
