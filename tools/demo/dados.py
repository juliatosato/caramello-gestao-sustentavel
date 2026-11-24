"""Synthetic, transactional SQLite fixtures for the portfolio demo."""

from contextlib import closing
from datetime import datetime, timedelta, timezone
from pathlib import Path
from random import Random
from sqlite3 import connect


# Quantities below are integer thousandths, including counts of individual units.
# Name, category, unit, typical daily use, minimum, lead time, safety stock, final balance
INSUMOS = [
    ("Café em grãos", "Café e bebidas", "kg", 1150, 4500, 5, 3000, 1800),
    ("Leite integral", "Laticínios", "L", 6500, 14000, 2, 8000, 9200),
    ("Bebida de aveia", "Café e bebidas", "L", 1700, 5000, 4, 2500, 6400),
    ("Pão de queijo", "Panificação", "un", 18000, 40000, 3, 22000, 27000),
    ("Croissant", "Panificação", "un", 12000, 28000, 2, 16000, 36000),
    ("Chocolate em pó", "Café e bebidas", "kg", 350, 1800, 5, 900, 2700),
    ("Polpa de maracujá", "Frutas e polpas", "kg", 820, 3000, 4, 1500, 4000),
    ("Copos de papel", "Embalagens", "un", 26000, 105000, 6, 55000, 154000),
    ("Tampas para copo", "Embalagens", "un", 24000, 95000, 6, 52000, 121000),
    ("Xarope de baunilha", "Café e bebidas", "ml", 155000, 600000, 5, 350000, 810000),
]

PERDAS = [
    (1, 7, 1250, "Vencimento"),
    (3, 11, 3000, "Preparo excessivo"),
    (6, 16, 600, "Armazenamento inadequado"),
    (4, 23, 4000, "Preparo excessivo"),
    (0, 27, 450, "Armazenamento inadequado"),
    (1, 34, 1800, "Vencimento"),
    (3, 39, 6000, "Preparo excessivo"),
    (6, 43, 750, "Armazenamento inadequado"),
    (4, 47, 4000, "Preparo excessivo"),
    (8, 50, 5000, "Embalagem danificada"),
    (1, 52, 1100, "Vencimento"),
    (0, 56, 550, "Armazenamento inadequado"),
]


def seed(database: Path) -> None:
    horario_demo = timezone(timedelta(hours=-3))
    inicio = datetime.now(horario_demo).date() - timedelta(days=59)
    sorteio = Random(120)

    def data(dia, hora):
        return f"{inicio + timedelta(days=dia)} {hora:02}:00:00"

    with closing(connect(database.as_uri() + "?mode=rw", uri=True)) as db, db:
        db.execute("PRAGMA foreign_keys=ON")
        db.execute("BEGIN IMMEDIATE")
        for tabela in ("Insumos", "MovimentacoesEstoque", "RegistrosBorra", "Descartes", "AspNetUsers"):
            if db.execute(f"SELECT EXISTS (SELECT 1 FROM {tabela})").fetchone()[0]:
                raise RuntimeError("Use um banco demonstrativo recém-migrado e vazio.")

        for indice, (nome, categoria, unidade, diario, minimo, prazo, seguranca, final) in enumerate(INSUMOS):
            id_insumo = db.execute(
                """INSERT INTO Insumos (Nome,Categoria,Unidade,Quantidade,EstoqueMinimo,
                       PrazoReposicaoDias,EstoqueSeguranca) VALUES (?,?,?,0,?,?,?)""",
                (nome, categoria, unidade, minimo, prazo, seguranca),
            ).lastrowid
            saldo = 0

            def movimento(tipo, quantidade, motivo, dia, hora):
                nonlocal saldo
                saldo += quantidade if tipo == "Entrada" else -quantidade
                if quantidade <= 0 or saldo < 0 or (unidade == "un" and quantidade % 1000):
                    raise ValueError("Movimento demonstrativo inválido.")
                db.execute(
                    """INSERT INTO MovimentacoesEstoque (InsumoId,Tipo,Quantidade,Unidade,Motivo,Data)
                       VALUES (?,?,?,?,?,?)""",
                    (id_insumo, tipo, quantidade, unidade, motivo, data(dia, hora)),
                )
                db.execute("UPDATE Insumos SET Quantidade=? WHERE Id=?", (saldo, id_insumo))

            consumo = {}
            for dia in range(60):
                if dia % 7 == 2:
                    continue
                valor = diario * sorteio.randint(75, 127) // 100
                consumo[dia] = max(1000, valor // 1000 * 1000) if unidade == "un" else valor
            perdas = {dia: (quantidade, motivo) for item, dia, quantidade, motivo in PERDAS if item == indice}

            # The last delivery leaves a predetermined closing balance for each product.
            for bloco in range(0, 60, 10):
                necessidade = sum(consumo.get(d, 0) + perdas.get(d, (0, ""))[0] for d in range(bloco, bloco + 10))
                reserva = final if bloco == 50 else seguranca
                compra = necessidade + reserva - saldo
                if compra > 0:
                    movimento("Entrada", compra, "Recebimento de fornecedor", bloco, 9)
                for dia in range(bloco, bloco + 10):
                    if dia in consumo:
                        movimento("Consumo", consumo[dia], "Produção e atendimento", dia, 15)
                    if dia in perdas:
                        quantidade, motivo = perdas[dia]
                        movimento("Descarte", quantidade, motivo, dia, 18)
            if saldo != final:
                raise ValueError("Saldo final diverge do histórico demonstrativo.")

        for dia in range(60):
            if dia % 7 == 2:
                continue
            db.execute(
                """INSERT INTO RegistrosBorra (Tipo,Quantidade,Unidade,Destinacao,Observacao,Data)
                   VALUES ('Gerada',?,'kg','','Separada após o preparo',?)""",
                (sorteio.randint(1200, 1900), data(dia, 17)),
            )
            if dia % 7 in (1, 5):
                db.execute(
                    """INSERT INTO RegistrosBorra (Tipo,Quantidade,Unidade,Destinacao,Observacao,Data)
                       VALUES ('Destinada',?,'kg',?,'Lote de demonstração',?)""",
                    (2400, "Compostagem" if dia % 7 == 1 else "Coleta especializada", data(dia, 19)),
                )

        for dia in range(4, 60, 7):
            db.execute(
                "INSERT INTO Descartes (Material,Quantidade,Unidade,Motivo,Data) VALUES (?,?,?,?,?)",
                ("Papelão de transporte", sorteio.randint(1500, 3400), "kg", "Triagem de recicláveis", data(dia, 16)),
            )
