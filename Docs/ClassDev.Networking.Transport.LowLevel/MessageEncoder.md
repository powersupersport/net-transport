# MessageEncoder

The message encoder is a class that allows you to encode different types of data into a stream of bytes. It uses intuitive methods to simplify its usage.

For those interested, the message encoder actually just a wrapper around the ```System.IO.BinaryReader``` and ```System.IO.BinaryReader``` and utilizes ```System.IO.MemoryStream``` to manage the buffer. You can read more about those in the [MSDN Docs](https://docs.microsoft.com/en-us/dotnet/api/system.io).

&nbsp;

## Properties

### ```MessageEncoder.position```
```long position { get; set; }```

Gets or sets the position of the virtual caret.

Example. If your stream looks like this (The caret is the ```|``` sign), getting the value would return 4 (the next cell to write):

```[0] [1] [2] [3] | [ ] [ ] [ ]```

Whilst setting the value to 2 would do this:

```[0] [1] | [2] [3] [ ] [ ] [ ]```

You can use that to allocate empty cells or overwrite data if needed.

**It's important to note that the caret position determines the length of the message when sending. If you do modify it, you should restore it to its original position afterwards as your message will get cut off. As in the following example:**

Original with a modified caret:

```[0] [1] [2] [3] | [4] [5] [ ]```

What is going to be sent out:

```[0] [1] [2] [3]```

&nbsp;

## Methods


### ```MessageEncoder.Encode ()```

Use any of these methods to encode data types.

// WIP


### ```MessageEncoder.Decode ()```

Use any of these methods to encode data types.

// WIP
