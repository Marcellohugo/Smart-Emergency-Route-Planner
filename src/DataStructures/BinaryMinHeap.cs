using System;
using System.Collections.Generic;

namespace SmartEmergencyRoutePlanner.DataStructures
{
    public struct HeapNode
    {
        public int VertexId { get; }
        public double Priority { get; }

        public HeapNode(int vertexId, double priority)
        {
            VertexId = vertexId;
            Priority = priority;
        }
    }

    public class BinaryMinHeap
    {
        private readonly List<HeapNode> _elements = new List<HeapNode>();

        public int Count => _elements.Count;
        public bool IsEmpty => _elements.Count == 0;

        public void Insert(int vertexId, double priority)
        {
            _elements.Add(new HeapNode(vertexId, priority));
            HeapifyUp(_elements.Count - 1);
        }

        public HeapNode ExtractMin()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

            HeapNode min = _elements[0];
            int lastIndex = _elements.Count - 1;
            _elements[0] = _elements[lastIndex];
            _elements.RemoveAt(lastIndex);

            if (_elements.Count > 0)
            {
                HeapifyDown(0);
            }

            return min;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_elements[index].Priority < _elements[parentIndex].Priority)
                {
                    Swap(index, parentIndex);
                    index = parentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        private void HeapifyDown(int index)
        {
            int count = _elements.Count;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild < count && _elements[leftChild].Priority < _elements[smallest].Priority)
                {
                    smallest = leftChild;
                }
                if (rightChild < count && _elements[rightChild].Priority < _elements[smallest].Priority)
                {
                    smallest = rightChild;
                }

                if (smallest != index)
                {
                    Swap(index, smallest);
                    index = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        private void Swap(int i, int j)
        {
            HeapNode temp = _elements[i];
            _elements[i] = _elements[j];
            _elements[j] = temp;
        }
    }
}
