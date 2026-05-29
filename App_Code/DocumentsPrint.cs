
public class DocumentsPrint
{
    public string DocNumber { get; set; }
    public int Copies { get; set; }

    public DocumentsPrint(string DocNumber, int Copies)
    {
        this.DocNumber = DocNumber;
        this.Copies = Copies;
    }
}