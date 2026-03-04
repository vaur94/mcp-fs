# MCP-FS Profesyonel Seviye Geliştirme Kararları (Bölüm 2)

Bu doküman, yalnızca Bölüm 2 sorularının net kararlarını içerir.
Format:
- **Karar**: Nihai cevap
- **Durum**: `Onaylandı (P0/P1)` veya `Ertelendi`

## A) Limitler / DoS / Güvenli Varsayılanlar

1. `fs.open` için server-side hard upper cap olacak mı?
- Karar: **Evet**. Request override olsa da server-side hard cap zorunlu.
- Durum: **Onaylandı (P0)**
- Not: Önerilen model: `effectiveMaxBytes = min(requestMaxBytes, configOpenMaxBytes, hardCapOpenBytes)`.

2. `openMaxLines` artırılabilir mi?
- Karar: **Config ile artırılabilir**, ama hard cap üzerinde olamaz.
- Durum: **Onaylandı (P0)**
- Not: Güvenli model: `effectiveMaxLines = min(request/window, configOpenMaxLines, hardCapOpenLines)`.

3. `fs.search` için ek caps eklensin mi?
- Karar: **Evet**. `maxFilesScanned`, `maxFileSizeForSearch`, `timeoutMs` eklenecek.
- Durum: **Onaylandı (P0)**

4. `fs.patch` için caps eklensin mi?
- Karar: **Evet**. `maxPatchBytes`, `maxEdits`, `maxFileSizeForPatch` eklenecek.
- Durum: **Onaylandı (P0)**

## B) Tool Set Genişletme (P0/P1)

5. v1.0 P0/P1 hedefi:
- `fs.stat`: **P0**
- `fs.readDir`: **P0**
- `fs.batchOpen`: **P1**
- `fs.patchPreview`: **P0**
- `fs.health/ping`: **P0**
- Durum: **Onaylandı**

6. `fs.readDir` gerekli mi?
- Karar: **Evet, gerekli**. `fs.scan` ağır kalabildiği için UI/agent akışında düşük maliyetli tek-seviye listeleme kritik.
- Durum: **Onaylandı (P0)**

## C) Ignore / .gitignore Subset Netliği

7. Desteklenen subset pattern listesi normative yazılsın mı?
- Karar: **Evet**. Desteklenen pattern seti açıkça “normative” olarak belgelenmeli.
- Durum: **Onaylandı (P0)**

8. Subdir `.gitignore` hiyerarşisi v1.0’a girsin mi?
- Karar: **Evet**, fakat fazlanmış rollout ile.
- Durum: **Onaylandı (P1)**

## D) Search Davranışı (Kalite)

9. Case-insensitive default kalsın mı?
- Karar: **Evet** (geriye dönük uyumluluk için). `caseSensitive=true` ile override devam.
- Durum: **Onaylandı (P0)**

10. Opsiyonel binary search modu olsun mu?
- Karar: **Evet, opsiyonel ve default kapalı**.
- Durum: **Onaylandı (P1)**

11. `before/afterLines` context alanları eklensin mi?
- Karar: **Evet**, kontrollü ve capped şekilde.
- Durum: **Onaylandı (P1)**
- Not: Hard caps olmadan açılmayacak.

## E) Open (Range) Davranışı

12. Byte-range asla olmayacak mı?
- Karar: **v1.0 kapsamında hayır**. `fs.open` line-range kalacak.
- Durum: **Onaylandı (P0)**

13. `fs.open` response’ta `totalLines`/`fileSize` eklensin mi?
- Karar: **Kısmi evet**.
- Durum: **Onaylandı**
- Uygulama kararı:
  - `fileSize`: **P0** (ucuz ve faydalı)
  - `totalLines`: **P1** (maliyetli; opsiyonel/isteğe bağlı hesaplama)

## F) Patch Ergonomisi ve Güvenlik

14. `replaceRangeBySearch` gibi yüksek seviye op eklensin mi?
- Karar: **Hayır**. Riskli ve nondeterministic.
- Durum: **Onaylandı (P0 - mevcut model korunur)**

15. EOL politikası katılaştırılsın mı?
- Karar: **Evet**. Dosyanın mevcut EOL stili korunacak, insert/replace metni write sırasında normalize edilecek.
- Durum: **Onaylandı (P0)**

16. BOM preserve istiyor muyuz?
- Karar: **Evet**. BOM varsa korunacak.
- Durum: **Onaylandı (P0)**

## G) Loglama / DX

17. stderr log formatı plain mi JSONL mi?
- Karar: **Default plain text**, opsiyonel `jsonl` modu.
- Durum: **Onaylandı (P1: jsonl modu)**

18. `quiet mode` ister miyiz?
- Karar: **Evet**, ama `logLevel=error` davranışının alias’ı şeklinde.
- Durum: **Onaylandı (P1)**

19. `requestId/correlationId` fs response içine eklensin mi?
- Karar: **Default hayır**. JSON-RPC `id` yeterli.
- Durum: **Onaylandı (P0 - eklenmeyecek)**
- Not: Gerekirse ileride debug modunda opsiyonel metadata olabilir.

## H) Packaging / Release Standardı

20. Asset naming standardı sabitlensin mi?
- Karar: **Evet**.
- Durum: **Onaylandı (P0)**
- Önerilen standart: `mcp-fs_<version>_<rid>.tar.gz` (veya platforma göre `.zip`).

21. Release’e `mcp-fs.config.json.sample` eklensin mi?
- Karar: **Evet**.
- Durum: **Onaylandı (P0)**

22. SBOM/provenance gerekli mi?
- Karar: **v1.0 için opsiyonel**.
- Durum: **Onaylandı (P1)**

## I) Modlar / Politika

23. “Dangerous mode” olacak mı?
- Karar: **Hayır**. Tek güvenli mod yaklaşımı korunacak.
- Durum: **Onaylandı (P0)**
- Not: `followSymlinks=true` olsa bile root dışı erişim yasak kalır.

24. Workspace root runtime’da değişebilir mi?
- Karar: **Hayır**. Runtime root immutable; değişiklik için process restart gerekir.
- Durum: **Onaylandı (P0)**

---

## Uygulama Öncelik Özeti

### P0 (v1.0 çekirdek)
- `fs.open` hard caps
- `fs.search` caps (`maxFilesScanned`, `maxFileSizeForSearch`, `timeoutMs`)
- `fs.patch` caps (`maxPatchBytes`, `maxEdits`, `maxFileSizeForPatch`)
- `fs.stat`, `fs.readDir`, `fs.patchPreview`, `fs.health/ping`
- `.gitignore subset` normative dokümantasyon
- `fs.open` line-range modelinin korunması
- `fs.open` response `fileSize`
- EOL normalize + BOM preserve
- Release asset naming standardı
- release sample config
- Dangerous mode yok; runtime root immutable

### P1 (geliştirilebilir / eklenebilir)
- `fs.batchOpen`
- subdir `.gitignore` hiyerarşisi
- opsiyonel binary search
- `before/afterLines` controlled context
- `fs.open` response `totalLines` (opsiyonel)
- log format `jsonl`
- `quiet` alias
- SBOM/provenance
