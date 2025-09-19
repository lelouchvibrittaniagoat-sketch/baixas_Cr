using System;
using System.Collections.Generic;
using System.Text.Json;
using API_CONTAS_A_RECEBER_BAIXAS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API_CONTAS_A_RECEBER_BAIXAS.Models_context;

public partial class ContasAReceberDbContext : DbContext
{
    public ContasAReceberDbContext()
    {
    }

    public ContasAReceberDbContext(DbContextOptions<ContasAReceberDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<NotaFiscaisDeSaida> NotaFiscaisDeSaida { get; set; }
    public virtual DbSet<NotasFiscaisStatus> NotasFiscaisStatus {  get; set; }
    public virtual DbSet<Filiais> Filiais { get; set; }
    public virtual DbSet<NotasFiscaisDevolucao> NotasFiscaisDevolucaos { get; set; }
    public virtual DbSet<Status> Status { get; set; }
    public virtual DbSet<BaixasCR> BaixasCR { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=192.168.0.250;Database=DB-CONTAS A RECEBER;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        var converter = new ValueConverter<List<Dictionary<string, List<string>>>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
            v => JsonSerializer.Deserialize<List<Dictionary<string, List<string>>>>(v, new JsonSerializerOptions())
        );

        var comparer = new ValueComparer<List<Dictionary<string, List<string>>>>(
            (c1, c2) => JsonSerializer.Serialize(c1, new JsonSerializerOptions()) == JsonSerializer.Serialize(c2, new JsonSerializerOptions()),
            c => c == null ? 0 : JsonSerializer.Serialize(c, new JsonSerializerOptions()).GetHashCode(),
            c => JsonSerializer.Deserialize<List<Dictionary<string, List<string>>>>(
                JsonSerializer.Serialize(c, new JsonSerializerOptions()), new JsonSerializerOptions())
        );



        modelBuilder.Entity<BaixasCR>(entity =>
        {
            entity.HasKey(e => new { e.data_baixa, e.filial, e.conta_contabil, e.rede }).HasName("baixas_cr_pkey");
            entity.ToTable("baixas_cr", "integracoes_sap");
            entity.Property(e => e.id).HasColumnName("id").ValueGeneratedOnAdd(); ;
            entity.Property(e => e.status).HasColumnName("status");
            entity.Property(e => e.arquivo_excel).HasColumnName("arquivo_excel");
            entity.Property(e => e.data_criacao).HasColumnName("data_criacao");
            entity.Property(e => e.data_Atualizacao).HasColumnName("data_atualizacao");
            entity.Property(e => e.nome_arquivo).HasColumnName("nome_arquivo");
            entity.Property(e => e.data_baixa).HasColumnName("data_baixa");
            entity.Property(e => e.conta_contabil).HasColumnName("conta_contabil");
            entity.Property(e => e.filial).HasColumnName("filial");
            entity.Property(e => e.rede).HasColumnName("rede");
            entity.Property(e => e.json_erros).HasConversion(converter).HasColumnName("json_erros").HasColumnType("json").Metadata.SetValueComparer(comparer);
            entity.Property(e => e.conta_contabil_efetiva).HasColumnName("conta_contabil_efetiva");
            entity.Property(e => e.nro_baixas).HasColumnName("nro_baixas");
            entity.Property(e => e.baixas_e_docs_vinculados).HasConversion(converter).HasColumnName("baixas_e_docs_vinculados").HasColumnType("json").Metadata.SetValueComparer(comparer); ;
        });
        modelBuilder.Entity<Filiais>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("filiais_pkey");

            entity.ToTable("filiais", "integracoes_sap");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NomeFilial).HasColumnName("nome_filial");
            entity.Property(e => e.NomeFilial).HasColumnType("character varying");
            entity.Property(e => e.IdSap).HasColumnName("id_sap");


        });
        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("status_baixas_pkey");

            entity.ToTable("status_baixas", "integracoes_sap");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("nome_status");
            entity.Property(e => e.Name).HasColumnType("character varying");


        });
        modelBuilder.Entity<NotaFiscaisDeSaida>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("nota_fiscais_de_saida_pkey");

            entity.ToTable("nota_fiscais_de_saida", "integracoes_sap");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bplid)
                .HasColumnType("character varying")
                .HasColumnName("BPLId");
            entity.Property(e => e.Bplname)
                .HasColumnType("character varying")
                .HasColumnName("BPLName");
            entity.Property(e => e.Canceled).HasMaxLength(1);
            entity.Property(e => e.CardCode).HasColumnType("character varying");
            entity.Property(e => e.CardName).HasColumnType("character varying");
            entity.Property(e => e.CodRede)
                .HasColumnType("character varying")
                .HasColumnName("Cod_rede");
            entity.Property(e => e.DocDate).HasColumnType("character varying");
            entity.Property(e => e.DocEntry).HasMaxLength(20);
            entity.Property(e => e.DocNum).HasColumnType("character varying");
            entity.Property(e => e.DocStatus).HasColumnType("character varying");
            entity.Property(e => e.DocTotal).HasColumnType("money");
            entity.Property(e => e.FatherCard).HasColumnType("character varying");
            entity.Property(e => e.NomeFantasia).HasColumnType("character varying");
            entity.Property(e => e.NomeRede)
                .HasColumnType("character varying")
                .HasColumnName("Nome_Rede");
            entity.Property(e => e.NomeRedeCr)
                .HasColumnType("character varying")
                .HasColumnName("Nome_rede_cr");
            entity.Property(e => e.RedeCr)
                .HasColumnType("character varying")
                .HasColumnName("Rede_Cr");
            entity.Property(e => e.SaldoEmAberto)
                .HasColumnType("money")
                .HasColumnName("Saldo_em_aberto");
            entity.Property(e => e.Serial).HasColumnType("character varying");
            entity.Property(e => e.SeriesStr).HasColumnType("character varying");
        });

        modelBuilder.Entity<NotasFiscaisDevolucao>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notas_fiscais_devolucao_pkey");

            entity.ToTable("notas_fiscais_devolucao", "integracoes_sap");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bplid)
                .HasMaxLength(20)
                .HasColumnName("BPLId");
            entity.Property(e => e.Bplname)
                .HasColumnType("character varying")
                .HasColumnName("BPLName");
            entity.Property(e => e.Canceled).HasMaxLength(1);
            entity.Property(e => e.CardCode).HasColumnType("character varying");
            entity.Property(e => e.CardName).HasColumnType("character varying");
            entity.Property(e => e.CodRede)
                .HasColumnType("character varying")
                .HasColumnName("Cod_rede");
            entity.Property(e => e.DocDate).HasMaxLength(20);
            entity.Property(e => e.DocEntry).HasMaxLength(20);
            entity.Property(e => e.DocNum).HasColumnType("character varying");
            entity.Property(e => e.DocStatus).HasColumnType("character varying");
            entity.Property(e => e.DocTotal).HasColumnType("money");
            entity.Property(e => e.FatherCard).HasColumnType("character varying");
            entity.Property(e => e.NomeFantasia).HasColumnType("character varying");
            entity.Property(e => e.NomeRede)
                .HasColumnType("character varying")
                .HasColumnName("Nome_rede");
            entity.Property(e => e.NomeRedeCr)
                .HasColumnType("character varying")
                .HasColumnName("Nome_rede_cr");
            entity.Property(e => e.RedeCr)
                .HasColumnType("character varying")
                .HasColumnName("Rede_Cr");
            entity.Property(e => e.SaldoEmAberto)
                .HasColumnType("money")
                .HasColumnName("Saldo_em_aberto");
            entity.Property(e => e.Serial).HasColumnType("character varying");
            entity.Property(e => e.SeriesStr).HasColumnType("character varying");
        });
        modelBuilder.Entity<NotasFiscaisStatus>(entity =>
        {
            entity.HasKey(e => e.id).HasName("notas_fiscais_status_pkey");

            entity.ToTable("notas_fiscais_status", "integracoes_sap");

            entity.Property(e => e.id).HasColumnName("id");
            entity.Property(e => e.docNum).HasColumnName("doc_num");
            entity.Property(e => e.docEntry).HasColumnName("doc_entry");
            entity.Property(e => e.idBaixa).HasColumnName("id_baixa");
            entity.Property(e => e.jaBaixado).HasColumnName("ja_baixado");
            entity.Property(e => e.tipoDoc).HasColumnName("tipo_doc");
            entity.Property(e => e.docEntryContasAReceber).HasColumnName("doc_entry_contas_a_receber");
            entity.Property(e => e.docNumContasAReceber).HasColumnName("doc_num_contas_a_receber");
            entity.Property(e => e.cL).HasColumnName("cl");
            entity.Property(e => e.cancelado).HasColumnName("cancelado");
            // Armazena erros como JSON em uma coluna do tipo text/jsonb
            entity.Property(e => e.erros)
                .HasColumnName("erros")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                );

        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
