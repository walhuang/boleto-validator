// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

string[] boletos = {
    "84670000001 43590024020 02405000243 84221010811",
    "84670000001-7 43590024020-9 02405000243-5 84221010811-9",
    "2379 7 40430000124020 04480 5616862379 3601105800",
    "2379 0.4480 9  56168.62379 3 36011.05800 9 7 40430000124020"
};

foreach (string boleto in boletos)
{
    bool teste = BoletoValidator.ValidarBoleto(boleto, true);
    string resultado = "inválido";
    if (teste)
    {
        resultado = "válido";
    }
    Console.WriteLine($"boleto: {boleto} é: {resultado}");
}

Console.ReadLine();