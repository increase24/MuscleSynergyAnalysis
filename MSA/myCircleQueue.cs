using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSA
{
    class myCircleQueue<T>
    {
        public myCircleQueue(int maxsize)
        {
            if (maxsize < 1)
            {
                throw new IndexOutOfRangeException("传入的队列长度不能小于1");
            }
            this.MaxSize = maxsize + 1;
            this.Data = new T[this.MaxSize];
            this.front = 0;//队列的头
            this.rear = 0;//队尾,下一个EnQueue的位置
        }
        public int MaxSize { get; set; }
        public T[] Data { get; set; }
        public int front { get; set; }//队头指针
        public int rear { get; set; } //队尾指针
        public void myEnQueue(T value)
        {

            //判断队列是否已满(队列中存在maxsize个或者说MaxSize-1个数据)
            if ((this.rear + 1) % this.MaxSize == this.front)
            {
                this.front = (this.front + 1) % this.MaxSize;
                this.Data[rear] = value;
                this.rear = (this.rear + 1) % this.MaxSize;
            }
            else
            {
                this.Data[rear] = value;
                this.rear = (this.rear + 1) % this.MaxSize;
            }
        }

        public T myDeQueue()
        {
            T value = default(T);
            //判断队列是否为空
            if (this.rear == this.front)
                return value;
            else
            {
                value = Data[front];
                Data[front] = default(T);
                front = (front + 1) % MaxSize;
                return value;
            }
        }
        public T[] toArray()
        {
            T[] output = new T[MaxSize - 1];
            for (int i = 0; i < ((rear - front) + MaxSize) % MaxSize; i++)
            {
                output[i] = Data[(front + i) % MaxSize];
            }
            return output;
        }

        public T[,] to1dMatrix()
        {
            T[,] output = new T[MaxSize - 1, 1];
            for (int i = 0; i < ((rear - front) + MaxSize) % MaxSize; i++)
            {
                output[i, 0] = Data[(front + i) % MaxSize];
            }
            return output;
        }
    }
}
