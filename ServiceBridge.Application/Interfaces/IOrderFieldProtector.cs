namespace ServiceBridge.Application.Interfaces;

public interface IOrderFieldProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}
