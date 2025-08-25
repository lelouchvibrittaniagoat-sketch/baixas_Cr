using System;
using System.Collections.Generic;

namespace API_CONTAS_A_RECEBER_BAIXAS.Models;

public partial class NotaFiscaisDeSaida
{
    public int Id { get; set; }

    public string? DocDate { get; set; }

    public string? Bplid { get; set; }

    public string? Bplname { get; set; }

    public string? Serial { get; set; }

    public decimal? DocTotal { get; set; }

    public string? CardCode { get; set; }

    public string? CardName { get; set; }

    public string? SeriesStr { get; set; }

    public string? FatherCard { get; set; }

    public string? DocStatus { get; set; }

    public string? DocNum { get; set; }

    public string? CodRede { get; set; }

    public string? NomeRede { get; set; }

    public decimal? SaldoEmAberto { get; set; }

    public string? RedeCr { get; set; }

    public string? Canceled { get; set; }

    public string? DocEntry { get; set; }

    public string? NomeFantasia { get; set; }

    public string? NomeRedeCr { get; set; }

}
