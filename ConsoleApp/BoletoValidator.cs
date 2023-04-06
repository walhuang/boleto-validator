// See https://aka.ms/new-console-template for more information
using System.Linq;

public static class BoletoValidator
{
    //public static string TransformarEmLinhaDigitavel(string codigoBoleto, bool isConcessionaria = false)
    //{
    //    string linhaDigitavel = string.Empty;
    //    linhaDigitavel = new string(codigoBoleto.Where(Char.IsDigit).ToArray());

    //    if (linhaDigitavel.Length <= 44)// não é true
    //    {
    //        //código de barras
    //        if (!isConcessionaria)
    //        {
    //            //5 digito é o DV original
    //            char DVoriginal = linhaDigitavel[4];
    //        }
    //        else
    //        {
    //            //Boleto de Concessionária (Sempre começa com 8)
    //            //4 digito pe o DV original
    //            char DVoriginal = linhaDigitavel[3];
    //            //
    //            char identificacaoProduto = linhaDigitavel[0];
    //            char identificacaoSegmento = linhaDigitavel[1];
    //            char identificacaoValorRealOuReferencia = linhaDigitavel[2];
    //            string valor = linhaDigitavel.Substring(4, 11);
    //            //primeiro
    //            string identificacaoEmpresaOuOrgao = linhaDigitavel.Substring(15, 4);
    //            string campoLivre = linhaDigitavel.Substring(19, 25);

    //            if (identificacaoSegmento == 6)
    //            {
    //                //identificado pelo CNPJ
    //                identificacaoEmpresaOuOrgao = linhaDigitavel.Substring(15, 8);
    //                campoLivre = linhaDigitavel.Substring(23, 21);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        //já está em linha digitável
    //    }

    //    return linhaDigitavel;
    //}

    public static bool ValidarBoleto(string codigo, bool validarBlocos = false)
    {
        if (string.IsNullOrEmpty(codigo))
        {
            //vazio
            return false;
        }
        string codigoNumerico = new string(codigo.Where(char.IsDigit).ToArray());

        //se primeiro dígito é 8 é um boleto de arrecadação/concesionária
        if (codigoNumerico[0] == '8')
        {
            return ValidarBoletoArrecadacao(codigo, validarBlocos);
        }
        return ValidarBoletoBancario(codigo, validarBlocos);
    }

    #region Boleto Arrecadação
    public static bool ValidarBoletoArrecadacao(string codigo, bool validarBlocos = false)
    {
        string codigoNumerico = new string(codigo.Where(char.IsDigit).ToArray());
        if (codigoNumerico.Length == 44)
        {
            return ValidarBoletoArrecadacaoCodigoBarras(codigoNumerico);
        }
        if (codigoNumerico.Length == 48)
        {
            return ValidarBoletoArrecadaaoLinhaDigitavel(codigoNumerico, validarBlocos);
        }

        return false;
    }

    private static bool ValidarBoletoArrecadacaoCodigoBarras(string codigo)
    {
        if (codigo.Length != 44 && codigo[0] != 8)
        {
            return false;
        }
        decimal codigoMoeda = decimal.Parse(codigo[2].ToString());
        decimal DV = decimal.Parse(codigo[3].ToString());
        string bloco = codigo.Substring(0, 3) + codigo.Substring(4);
        decimal? modulo = null;
        if (codigoMoeda == 6 || codigoMoeda == 7)
        {
            modulo = Modulo10(bloco);
        }
        else if (codigoMoeda == 8 || codigoMoeda == 9)
        {
            modulo = Modulo11Arrecadacao(bloco);
        }
        else
        {
            return false;
        }
        return modulo.Value == DV;
    }

    private static bool ValidarBoletoArrecadaaoLinhaDigitavel(string codigo, bool validarBlocos = false)
    {
        if (codigo.Length != 48 && codigo[0] != 8)
        {
            return false;
        }
        bool validDV = ValidarBoletoArrecadacaoCodigoBarras(ConverterParaBoletoArrecadacaoCodigoBarras(codigo));
        if (!validDV)
        {
            return validDV;
        }
        decimal codigoMoeda = decimal.Parse(codigo[2].ToString());

        List<BlocosBoleto> blocos = new List<BlocosBoleto>();
        int localizacaoDV = 11;
        for (int index = 0; index < 4; index++)
        {
            int inicioBloco = (11 * (index)) + index;
            int tamanhoBloco = 11;

            if (index != 0)
            {
                localizacaoDV += 12;

            }

#if DEBUG
            Console.WriteLine($"Localizacao DV: {localizacaoDV}, DV extraido: {codigo.Substring(localizacaoDV, 1)}");
#endif

            blocos.Add(
             new BlocosBoleto
             {
                 Numeros = codigo.Substring(inicioBloco, tamanhoBloco),
                 DV = decimal.Parse(codigo.Substring(localizacaoDV, 1))
             });
        }


        bool validBlocos = false;
        if (codigoMoeda == 6 || codigoMoeda == 7)
        {
            validBlocos = validarBlocos == true ? blocos.All(x => Modulo10(x.Numeros) == x.DV) : true;
            Console.WriteLine($"boleto: {codigo}");

#if DEBUG
            foreach (BlocosBoleto blocosBoleto in blocos)
            {
                Console.WriteLine($"Validando bloco: {blocosBoleto.Numeros}, DV calculado: {Modulo10(blocosBoleto.Numeros)}, DV: {blocosBoleto.DV}");
            }
#endif
        }
        else if (codigoMoeda == 8 || codigoMoeda == 9)
        {
            validBlocos = validarBlocos == true ? blocos.All(x => Modulo11Arrecadacao(x.Numeros) == x.DV) : true;
        }
        else
        {
            return false;
        }

        return validBlocos && validDV;
    }


    private static string ConverterParaBoletoArrecadacaoCodigoBarras(string codigo)
    {
        string codigoBarras = string.Empty;
        for (int index = 0; index < 4; index++)
        {
            int inicioBloco = (11 * (index)) + index;
            int tamanhoBloco = 11;
            codigoBarras += codigo.Substring(inicioBloco, tamanhoBloco);
        }

        return codigoBarras;
    }
    #endregion

    #region Boleto Bancário
    public static bool ValidarBoletoBancario(string codigo, bool validarBlocos = false)
    {
        string codigoNumerico = new string(codigo.Where(char.IsDigit).ToArray());
        if (codigoNumerico.Length == 44)
        {
            return ValidarBoletoBancarioCodigoBarras(codigoNumerico);
        }
        if (codigoNumerico.Length == 47)
        {
            return ValidarBoletoBancarioLinhaDigitavel(codigoNumerico, validarBlocos);
        }

        return false;
    }

    private static bool ValidarBoletoBancarioCodigoBarras(string codigo)
    {
        if (codigo.Length != 44)
        {
            return false;
        }
        decimal DV = decimal.Parse(codigo[4].ToString());
        string bloco = codigo.Substring(0, 4) + codigo.Substring(5);
        return Modulo11Bancario(bloco) == DV;
    }

    private static bool ValidarBoletoBancarioLinhaDigitavel(string codigo, bool validarBlocos = false)
    {
        if (codigo.Length != 47)
        {
            return false;
        }

        List<BlocosBoleto> blocos = new List<BlocosBoleto>
        {
            new BlocosBoleto
            {
                Numeros = codigo.Substring(0, 9),
                DV = decimal.Parse(codigo[9].ToString()),
            },
            new BlocosBoleto
            {
                Numeros = codigo.Substring(10, 10),
                DV = decimal.Parse(codigo[20].ToString())
            },
            new BlocosBoleto
            {
                Numeros = codigo.Substring(21, 10),
                DV = decimal.Parse(codigo[31].ToString())
            }
        };

#if DEBUG
        foreach (BlocosBoleto blocosBoleto in blocos)
        {
            Console.WriteLine($"Validando bloco: {blocosBoleto.Numeros}, DV calculado: {Modulo10(blocosBoleto.Numeros)}, DV: {blocosBoleto.DV}");
        }
#endif

        bool validBlocos = validarBlocos ? blocos.All(x => Modulo10(x.Numeros) == x.DV) : true;
        bool validDV = ValidarBoletoBancarioCodigoBarras(ConverterParaBoletoBancarioCodigoBarras(codigo));

        return validBlocos && validDV;
    }

    private static string ConverterParaBoletoBancarioCodigoBarras(string codigo)
    {
        string codigoBarras = string.Empty;

        codigoBarras += codigo.Substring(0, 3); //Identificação Banco
        codigoBarras += codigo.Substring(3, 1); //Código da Moeda
        codigoBarras += codigo.Substring(32, 1); //DV
        codigoBarras += codigo.Substring(33, 4); //Fator Vencimento
        codigoBarras += codigo.Substring(37, 10); //Valor Nominal
        codigoBarras += codigo.Substring(4, 5); //Campo Livre 1
        codigoBarras += codigo.Substring(10, 10); //Campo Livre 2
        codigoBarras += codigo.Substring(21, 10); //Campo Livre 3

        return codigoBarras;
    }
    #endregion


    #region Modulo
    private static decimal Modulo10(string bloco)
    {
        decimal[] codigo = bloco.Select(x => decimal.Parse(x.ToString())).Reverse().ToArray();
        decimal index = 0;
        decimal somatorio = codigo.Aggregate(0, (decimal acc, decimal current) =>
        {
            decimal soma = current * (((index + 1) % 2) + 1);
            soma = soma > 9 ? Math.Truncate(soma / 10) + (soma % 10) : soma;
            index++;
            return acc + soma;
        });
        return (Math.Ceiling(somatorio / 10) * 10) - somatorio;
    }

    private static decimal Modulo11Arrecadacao(string bloco)
    {
        //TODO TESTE MOEDA DIFERENTE
        decimal[] codigo = bloco.Select(x => decimal.Parse(x.ToString())).Reverse().ToArray();
        decimal multiplicador = 2;
        decimal somatorio = codigo.Aggregate(0, (decimal acc, decimal current) =>
        {
            decimal soma = current * multiplicador;
            multiplicador = multiplicador == 9 ? 2 : multiplicador + 1;
            return acc + soma;
        });
        decimal restoDivisao = somatorio % 11;

        if (restoDivisao == 0 || restoDivisao == 1)
        {
            return 0;
        }

        if (restoDivisao == 10)
        {
            return 1;
        }

        decimal DV = 11 - restoDivisao;
        return DV;
    }

    private static decimal Modulo11Bancario(string bloco)
    {
        decimal[] codigo = bloco.Select(x => decimal.Parse(x.ToString())).Reverse().ToArray();
        decimal multiplicador = 2;
        decimal somatorio = codigo.Aggregate(0, (decimal acc, decimal current) =>
        {
            decimal soma = current * multiplicador;
            multiplicador = multiplicador == 9 ? 2 : multiplicador + 1;
            return acc + soma;
        });

        decimal restoDivisao = somatorio % 11;
        decimal DV = 11 - restoDivisao;

        if (DV == 0 || DV == 10 || DV == 11)
        {
            return 1;
        }

        return DV;
    }
    #endregion

    public class BlocosBoleto
    {
        public string Numeros { get; set; }
        public decimal DV { get; set; }
    }
}