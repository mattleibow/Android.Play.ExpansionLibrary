using System;

/**
 * Exception thrown when encountering an invalid Base64 input character.
 * 
 * @author nelson
 */
public class Base64DecoderException : Exception
{
    public Base64DecoderException()
    {
    }

    public Base64DecoderException(string s)
        : base(s)
    {
    }

    private static long serialVersionUID = 1L;
}
