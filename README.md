# RCP-WT2

Výrobní a vážicí systém pro řízení receptur, plánování výroby a sledování průběhu vážení.

## Funkce

* Správa receptur
* Správa zakázek
* Vážení surovin
* Podpora základových receptur
* Komunikace s průmyslovými váhami
* Komunikace s MySQL databází
* OPC UA komunikace (volitelné, tato verze neobsahuje)
* Evidence výroby
* Přehled výrobních dávek
* Správa uživatelů a oprávnění
* Dotykové ovládání optimalizované pro výrobní terminály

## Použité technologie

* C#
* WinUI 3
* .NET
* MySQL
* OPC UA
* Visual Studio 2026

## Struktura projektu

* Assets – ikony, obrázky a grafické prostředky
* MySQL – databázová vrstva
* PomocneTridy – sdílené pomocné třídy
* SerialComm – komunikace se zařízeními
* Vizualizace – uživatelské rozhraní aplikace

## Požadavky

* Windows 10 / Windows 11
* .NET SDK
* Visual Studio 2026
* Přístup k MySQL databázi
* Připojení k výrobním zařízením dle konfigurace

## Sestavení projektu

1. Naklonovat repozitář.
2. Otevřít soubor RCP_WT1.slnx.
3. Obnovit NuGet balíčky.
4. Spustit Build Solution.
5. Spustit aplikaci.

## Autor
Tomáš Němec

