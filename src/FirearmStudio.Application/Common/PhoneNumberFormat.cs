namespace FirearmStudio.Application.Common;

public static class PhoneNumberFormat
{
    // Leading '+', first digit 1-9, then 7-14 more digits (total 8-15 digits). E.164.
    public const string E164Pattern = @"^\+[1-9]\d{7,14}\z";
}
