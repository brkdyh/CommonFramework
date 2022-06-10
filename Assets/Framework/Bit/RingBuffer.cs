//[0,1,2,3,4] ptr=2,l=3

using System;
using System.Text;
/// <summary>
/// 环形缓冲区RingBuffer
/// </summary>
public class RingBuffer
{
    public int read_ptr = 0;                //读指针
    public ulong readLoop = 0;               //读循环次数

    public int write_ptr = 0;               //写指针
    public ulong writeLoop = 0;              //写循环次数

    public byte[] buffer = null;
    public int buffer_length { get { return buffer.Length; } }
    public int buffer_head { get { return 0; } }
    public int buffer_end { get { return buffer == null ? 0 : buffer.Length - 1; } }

    public int writePtr2end { get { return buffer_end - write_ptr + 1; } }             //写指针到buffer尾距离
    public int write_rest
    {
        get
        {
            if (isFull(read_ptr, write_ptr))
                return 0;
            else
                return writePtr2end + head2readPtr;
        }
    }              //剩余写入长度

    public int head2readPtr { get { return read_ptr; } }                               //buffer头到读指针距
    public int readPtr2end { get { return buffer_end - read_ptr + 1; } }               //读指针到buffer尾距离
    //剩余读取长度
    public int read_rest
    {
        get
        {
            if (write_ptr > read_ptr)
                return write_ptr - read_ptr;
            else if (write_ptr < read_ptr)
                return readPtr2end + write_ptr;
            else
            {
                var rest = isFull(read_ptr, write_ptr) ? buffer_length : 0;
                return rest;
            }
        }
    }                                     

    public RingBuffer(int size)
    {
        buffer = new byte[size];
    }

    //缓冲区是否已写满
    bool isFull(int rPtr, int wPtr)
    {
        return (writeLoop > readLoop) && rPtr <= wPtr;
    }

    bool writeOverflow(int rPtr, int wPtr)
    {
        return (writeLoop > readLoop) && rPtr < wPtr;
    }

    //缓冲区是否为空
    bool isEmpty(int rPtr, int wPtr)
    {
        return (readLoop >= writeLoop) && rPtr == wPtr;
    }

    public bool MoveWritePtr(int length)
    {
        var wPtr = write_ptr + length;
        if (wPtr > buffer_end)
        {//指针循环
            writeLoop++;
            wPtr = (length - 1) - buffer_end + write_ptr;
        }

        if (writeOverflow(read_ptr, wPtr))
            return false;

        write_ptr = wPtr;

        return true;
    }


    public void Write(byte[] data)
    {
        if (data == null)
            return;

        int length = data.Length;
        var ori_w_ptr = write_ptr;

        if (!CouldWrite(length))
            throw new Exception(string.Format("缓冲区剩余长度({0})不足,无法写入数据({1})。", write_rest, data.Length));

        if (!MoveWritePtr(length))
            throw new Exception(string.Format("无法移动写指针,写入数据失败"));

        int cycle_size = length - writePtr2end;
        if (cycle_size > 0)
        {
            Array.Copy(data, 0, buffer, ori_w_ptr, writePtr2end);
            Array.Copy(data, writePtr2end, buffer, 0, cycle_size);
        }
        else
            Array.Copy(data, 0, buffer, ori_w_ptr, length);
    }


    public void MoveReadPtr(int length)
    {
        var rPtr = read_ptr + length;
        if (rPtr > buffer_end)
        {
            readLoop++;
            rPtr = (length - 1) - buffer_end + read_ptr;
        }
        read_ptr = rPtr;
    }

    public byte[] Read(int length)
    {
        byte[] data = new byte[length];

        if (Read(data, 0, length) == -1)
            return new byte[0];

        return data;
    }

    public int Read(byte[] dst_array, int offset, int length)
    {
        if (isEmpty(read_ptr, write_ptr))
            return -1;

        if (!CouldRead(length))
            return -1;

        int cycle_size = length - readPtr2end;

        if (cycle_size > 0)
        {
            Array.Copy(buffer, read_ptr, dst_array, offset, readPtr2end);
            Array.Copy(buffer, 0, dst_array, offset + readPtr2end, cycle_size);
        }
        else
            Array.Copy(buffer, read_ptr, dst_array, 0, length);

        MoveReadPtr(length);
        return length;
    }
    
    public bool CouldRead(int length) { return read_rest >= length; }
    public bool CouldWrite(int length) { return write_rest >= length; }

    public void Extend()
    {

    }

    public string OutputBuffer()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("RingBuffer [Size = {0}] =>\n", buffer_length);
        for (int i = 0; i < buffer.Length; i++)
        {
            if (i == 0)
                sb.AppendFormat("[{0}]", buffer[i]);
            else
                sb.AppendFormat("-[{0}]", buffer[i]);
        }

        return sb.ToString();
    }
}
