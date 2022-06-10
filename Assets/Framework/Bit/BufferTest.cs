using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferTest : MonoBehaviour
{
    [Header("写指针")]
    public int write_ptr;
    [Header("读指针")]
    public int read_ptr;

    [Header("缓存头")]
    public int buffer_head = 0;
    [Header("缓存尾")]
    public int buffer_end;

    [Header("写指针-缓存尾")]
    public int writePtr2end;

    [Header("剩余写入容量")]
    public int write_rest;

    [Header("缓存头-读指针")]
    public int head2readPtr;
    [Header("读指针-缓存尾")]
    public int readPtr2end;

    RingBuffer buffer;
    void Start()
    {
        Debug.Log(ulong.MaxValue);
        buffer = new RingBuffer(10);

        byte[] data1 = new byte[5];
        for (int i = 0; i < 5; i++)
        {
            data1[i] = (byte)i;
        }

        //Debug.Log(buffer.CouldWrite(data1.Length));
        buffer.Write(data1);
        Debug.Log(buffer.OutputBuffer());

        //Debug.Log("读指针 => " + buffer.read_ptr);
        //Debug.Log("写指针 => " + buffer.write_ptr);

        var rd = buffer.Read(5);
        Debug.Log("" + rd[0] + rd[1] + rd[2] + rd[3] + rd[4]);

        Debug.Log(buffer.OutputBuffer());

        //Debug.Log("读指针 => " + buffer.read_ptr);
        //Debug.Log("写指针 => " + buffer.write_ptr);

        byte[] data2 = new byte[10];
        for (int i = 0; i < 10; i++)
        {
            data2[i] = (byte)i;
        }

        buffer.Write(data2);

        Debug.Log(buffer.OutputBuffer());

        var rd1 = buffer.Read(10);
        //Debug.Log(rd1.Length);
        Debug.Log("" + rd1[0] + rd1[1] + rd1[2] + rd1[3] + rd1[4]
            + rd1[5] + rd1[6] + rd1[7] + rd1[8] + rd1[9]);
    }

    // Update is called once per frame
    void Update()
    {
        read_ptr = buffer.read_ptr;
        write_ptr = buffer.write_ptr;
        buffer_end = buffer.buffer_end;
        writePtr2end = buffer.writePtr2end;
        write_rest = buffer.write_rest;
        head2readPtr = buffer.head2readPtr;
        readPtr2end = buffer.readPtr2end;
    }
}
