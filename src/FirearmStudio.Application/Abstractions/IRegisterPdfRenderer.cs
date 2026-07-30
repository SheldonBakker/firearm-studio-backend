using FirearmStudio.Application.Registers;

namespace FirearmStudio.Application.Abstractions;

public interface IRegisterPdfRenderer
{
    byte[] Render(RegisterDocument document);
}
