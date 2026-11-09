using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class EvolucaoPerfisViagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // Older PostgreSQL installations may already contain some objects from
                // EnsureCreated()/manual provisioning while the EF history is incomplete.
                // Reconcile additively: never drop or rebuild application objects.
                migrationBuilder.Sql("""
                    ALTER TABLE public."Vans"
                        ADD COLUMN IF NOT EXISTS "MotoristaId" integer,
                        ADD COLUMN IF NOT EXISTS "RotaId" integer;

                    ALTER TABLE public."Usuarios"
                        ADD COLUMN IF NOT EXISTS "FotoPerfil" character varying(120),
                        ADD COLUMN IF NOT EXISTS "Tema" character varying(10) NOT NULL DEFAULT 'claro';

                    ALTER TABLE public."PresencasViagem"
                        ADD COLUMN IF NOT EXISTS "ConfirmadoEm" timestamp with time zone,
                        ADD COLUMN IF NOT EXISTS "StatusConfirmacao" boolean;

                    ALTER TABLE public."Motoristas"
                        ADD COLUMN IF NOT EXISTS "UsuarioId" integer;

                    ALTER TABLE public."Alunos"
                        ADD COLUMN IF NOT EXISTS "UsuarioId" integer;
                    """);

                // Fail clearly rather than silently accepting a same-named column
                // whose type is incompatible with the EF model.
                migrationBuilder.Sql("""
                    DO $migration$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'Vans'
                              AND column_name IN ('MotoristaId', 'RotaId') AND data_type <> 'integer')
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'Motoristas'
                              AND column_name = 'UsuarioId' AND data_type <> 'integer')
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'Alunos'
                              AND column_name = 'UsuarioId' AND data_type <> 'integer')
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'Usuarios'
                              AND column_name = 'FotoPerfil'
                              AND (data_type <> 'character varying' OR character_maximum_length <> 120))
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'Usuarios'
                              AND column_name = 'Tema'
                              AND (data_type <> 'character varying' OR character_maximum_length <> 10))
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'PresencasViagem'
                              AND column_name = 'ConfirmadoEm' AND data_type <> 'timestamp with time zone')
                        OR EXISTS (SELECT 1 FROM information_schema.columns
                            WHERE table_schema = 'public' AND table_name = 'PresencasViagem'
                              AND column_name = 'StatusConfirmacao' AND data_type <> 'boolean')
                        THEN
                            RAISE EXCEPTION 'EvolucaoPerfisViagens: existing column type differs from the expected PostgreSQL schema; no migration changes were committed.';
                        END IF;
                    END
                    $migration$;
                    """);

                migrationBuilder.Sql("""
                    CREATE INDEX IF NOT EXISTS "IX_Viagens_MotoristaId" ON public."Viagens" ("MotoristaId");
                    CREATE INDEX IF NOT EXISTS "IX_Viagens_RotaId" ON public."Viagens" ("RotaId");
                    CREATE INDEX IF NOT EXISTS "IX_Viagens_VanId" ON public."Viagens" ("VanId");
                    CREATE INDEX IF NOT EXISTS "IX_Vans_MotoristaId" ON public."Vans" ("MotoristaId");
                    CREATE INDEX IF NOT EXISTS "IX_Vans_RotaId" ON public."Vans" ("RotaId");
                    CREATE INDEX IF NOT EXISTS "IX_PresencasViagem_AlunoId" ON public."PresencasViagem" ("AlunoId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_PresencasViagem_ViagemId_AlunoId"
                        ON public."PresencasViagem" ("ViagemId", "AlunoId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Motoristas_UsuarioId" ON public."Motoristas" ("UsuarioId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Alunos_UsuarioId" ON public."Alunos" ("UsuarioId");
                    """);

                migrationBuilder.Sql("""
                    DO $migration$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Alunos_Usuarios_UsuarioId' AND conrelid = 'public."Alunos"'::regclass) THEN
                            ALTER TABLE public."Alunos" ADD CONSTRAINT "FK_Alunos_Usuarios_UsuarioId"
                                FOREIGN KEY ("UsuarioId") REFERENCES public."Usuarios" ("Id") ON DELETE SET NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Motoristas_Usuarios_UsuarioId' AND conrelid = 'public."Motoristas"'::regclass) THEN
                            ALTER TABLE public."Motoristas" ADD CONSTRAINT "FK_Motoristas_Usuarios_UsuarioId"
                                FOREIGN KEY ("UsuarioId") REFERENCES public."Usuarios" ("Id") ON DELETE SET NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PresencasViagem_Alunos_AlunoId' AND conrelid = 'public."PresencasViagem"'::regclass) THEN
                            ALTER TABLE public."PresencasViagem" ADD CONSTRAINT "FK_PresencasViagem_Alunos_AlunoId"
                                FOREIGN KEY ("AlunoId") REFERENCES public."Alunos" ("Id") ON DELETE RESTRICT;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PresencasViagem_Viagens_ViagemId' AND conrelid = 'public."PresencasViagem"'::regclass) THEN
                            ALTER TABLE public."PresencasViagem" ADD CONSTRAINT "FK_PresencasViagem_Viagens_ViagemId"
                                FOREIGN KEY ("ViagemId") REFERENCES public."Viagens" ("Id") ON DELETE CASCADE;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Vans_Motoristas_MotoristaId' AND conrelid = 'public."Vans"'::regclass) THEN
                            ALTER TABLE public."Vans" ADD CONSTRAINT "FK_Vans_Motoristas_MotoristaId"
                                FOREIGN KEY ("MotoristaId") REFERENCES public."Motoristas" ("Id") ON DELETE SET NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Vans_Rotas_RotaId' AND conrelid = 'public."Vans"'::regclass) THEN
                            ALTER TABLE public."Vans" ADD CONSTRAINT "FK_Vans_Rotas_RotaId"
                                FOREIGN KEY ("RotaId") REFERENCES public."Rotas" ("Id") ON DELETE SET NULL;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Viagens_Motoristas_MotoristaId' AND conrelid = 'public."Viagens"'::regclass) THEN
                            ALTER TABLE public."Viagens" ADD CONSTRAINT "FK_Viagens_Motoristas_MotoristaId"
                                FOREIGN KEY ("MotoristaId") REFERENCES public."Motoristas" ("Id") ON DELETE RESTRICT;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Viagens_Rotas_RotaId' AND conrelid = 'public."Viagens"'::regclass) THEN
                            ALTER TABLE public."Viagens" ADD CONSTRAINT "FK_Viagens_Rotas_RotaId"
                                FOREIGN KEY ("RotaId") REFERENCES public."Rotas" ("Id") ON DELETE RESTRICT;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Viagens_Vans_VanId' AND conrelid = 'public."Viagens"'::regclass) THEN
                            ALTER TABLE public."Viagens" ADD CONSTRAINT "FK_Viagens_Vans_VanId"
                                FOREIGN KEY ("VanId") REFERENCES public."Vans" ("Id") ON DELETE RESTRICT;
                        END IF;
                    END
                    $migration$;
                    """);

                return;
            }

            migrationBuilder.AddColumn<int>(
                name: "MotoristaId",
                table: "Vans",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RotaId",
                table: "Vans",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfil",
                table: "Usuarios",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "Usuarios",
                maxLength: 10,
                nullable: false,
                defaultValue: "claro");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmadoEm",
                table: "PresencasViagem",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StatusConfirmacao",
                table: "PresencasViagem",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Motoristas",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Alunos",
                nullable: true);

            migrationBuilder.CreateIndex(name: "IX_Viagens_MotoristaId", table: "Viagens", column: "MotoristaId");
            migrationBuilder.CreateIndex(name: "IX_Viagens_RotaId", table: "Viagens", column: "RotaId");
            migrationBuilder.CreateIndex(name: "IX_Viagens_VanId", table: "Viagens", column: "VanId");
            migrationBuilder.CreateIndex(name: "IX_Vans_MotoristaId", table: "Vans", column: "MotoristaId");
            migrationBuilder.CreateIndex(name: "IX_Vans_RotaId", table: "Vans", column: "RotaId");
            migrationBuilder.CreateIndex(name: "IX_PresencasViagem_AlunoId", table: "PresencasViagem", column: "AlunoId");
            migrationBuilder.CreateIndex(name: "IX_PresencasViagem_ViagemId_AlunoId", table: "PresencasViagem", columns: new[] { "ViagemId", "AlunoId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Motoristas_UsuarioId", table: "Motoristas", column: "UsuarioId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Alunos_UsuarioId", table: "Alunos", column: "UsuarioId", unique: true);

            migrationBuilder.AddForeignKey(name: "FK_Alunos_Usuarios_UsuarioId", table: "Alunos", column: "UsuarioId", principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Motoristas_Usuarios_UsuarioId", table: "Motoristas", column: "UsuarioId", principalTable: "Usuarios", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_PresencasViagem_Alunos_AlunoId", table: "PresencasViagem", column: "AlunoId", principalTable: "Alunos", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_PresencasViagem_Viagens_ViagemId", table: "PresencasViagem", column: "ViagemId", principalTable: "Viagens", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Vans_Motoristas_MotoristaId", table: "Vans", column: "MotoristaId", principalTable: "Motoristas", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Vans_Rotas_RotaId", table: "Vans", column: "RotaId", principalTable: "Rotas", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_Viagens_Motoristas_MotoristaId", table: "Viagens", column: "MotoristaId", principalTable: "Motoristas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Viagens_Rotas_RotaId", table: "Viagens", column: "RotaId", principalTable: "Rotas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Viagens_Vans_VanId", table: "Viagens", column: "VanId", principalTable: "Vans", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // This migration reconciles pre-existing production objects as well as
                // creating missing ones. A blind Down could delete objects that predate
                // this migration, so PostgreSQL rollback is intentionally non-destructive.
                return;
            }

            migrationBuilder.DropForeignKey(name: "FK_Alunos_Usuarios_UsuarioId", table: "Alunos");
            migrationBuilder.DropForeignKey(name: "FK_Motoristas_Usuarios_UsuarioId", table: "Motoristas");
            migrationBuilder.DropForeignKey(name: "FK_PresencasViagem_Alunos_AlunoId", table: "PresencasViagem");
            migrationBuilder.DropForeignKey(name: "FK_PresencasViagem_Viagens_ViagemId", table: "PresencasViagem");
            migrationBuilder.DropForeignKey(name: "FK_Vans_Motoristas_MotoristaId", table: "Vans");
            migrationBuilder.DropForeignKey(name: "FK_Vans_Rotas_RotaId", table: "Vans");
            migrationBuilder.DropForeignKey(name: "FK_Viagens_Motoristas_MotoristaId", table: "Viagens");
            migrationBuilder.DropForeignKey(name: "FK_Viagens_Rotas_RotaId", table: "Viagens");
            migrationBuilder.DropForeignKey(name: "FK_Viagens_Vans_VanId", table: "Viagens");

            migrationBuilder.DropIndex(name: "IX_Viagens_MotoristaId", table: "Viagens");
            migrationBuilder.DropIndex(name: "IX_Viagens_RotaId", table: "Viagens");
            migrationBuilder.DropIndex(name: "IX_Viagens_VanId", table: "Viagens");
            migrationBuilder.DropIndex(name: "IX_Vans_MotoristaId", table: "Vans");
            migrationBuilder.DropIndex(name: "IX_Vans_RotaId", table: "Vans");
            migrationBuilder.DropIndex(name: "IX_PresencasViagem_AlunoId", table: "PresencasViagem");
            migrationBuilder.DropIndex(name: "IX_PresencasViagem_ViagemId_AlunoId", table: "PresencasViagem");
            migrationBuilder.DropIndex(name: "IX_Motoristas_UsuarioId", table: "Motoristas");
            migrationBuilder.DropIndex(name: "IX_Alunos_UsuarioId", table: "Alunos");

            migrationBuilder.DropColumn(name: "MotoristaId", table: "Vans");
            migrationBuilder.DropColumn(name: "RotaId", table: "Vans");
            migrationBuilder.DropColumn(name: "FotoPerfil", table: "Usuarios");
            migrationBuilder.DropColumn(name: "Tema", table: "Usuarios");
            migrationBuilder.DropColumn(name: "ConfirmadoEm", table: "PresencasViagem");
            migrationBuilder.DropColumn(name: "StatusConfirmacao", table: "PresencasViagem");
            migrationBuilder.DropColumn(name: "UsuarioId", table: "Motoristas");
            migrationBuilder.DropColumn(name: "UsuarioId", table: "Alunos");
        }
    }
}
