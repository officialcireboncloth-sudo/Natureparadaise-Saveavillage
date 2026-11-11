// Apply only the prepared game-data rows, preserving the established CSV schema.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../..');
const dependencyRoot=process.env.CODEX_PRIMARY_RUNTIME_NODE_MODULES
  ?? 'C:/Users/h/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
// The host denies junction creation. Resolve the public package entry from its installed parent.
const runtimeRequire=createRequire(path.join(dependencyRoot,'../package.json'));
const { Workbook }=await import(pathToFileURL(runtimeRequire.resolve('@oai/artifact-tool')).href);
const integration=path.join(root,'ArtSource/Crops/Integration');
const prefix=process.argv[2] ?? '';
if(!/^[A-Za-z0-9_]*$/.test(prefix)) throw Error('Invalid CSV plan prefix');
const plan=JSON.parse(await fs.readFile(path.join(integration,prefix+'CsvEditPlan.json'),'utf8'));
const evidence=[];
for(const edit of plan){
  const target=path.join(root,edit.path);
  const book=await Workbook.fromCSV(await fs.readFile(target,'utf8'),{sheetName:'Data'});
  const sheet=book.worksheets.getItemAt(0);
  const matrix=[edit.header,...edit.rows];
  sheet.getRangeByIndexes(0,0,matrix.length,edit.header.length).values=matrix;
  book.recalculate();
  const actual=sheet.getRangeByIndexes(0,0,matrix.length,edit.header.length).values;
  for(let row=0;row<matrix.length;row++)for(let col=0;col<edit.header.length;col++){
    if(String(actual[row][col]??'')!==String(matrix[row][col]??''))throw Error(`CSV value changed at ${edit.path} ${row},${col}`);
  }
  const inspected=await book.inspect({kind:'table',range:'Data!A1:F4',include:'values',tableMaxRows:4,tableMaxCols:6,maxChars:1800});
  evidence.push({path:edit.path,rows:matrix.length-1,columns:edit.header.length,inspection:inspected.ndjson});
  // RFC 4180 text export from the authored range; no formulas, styles or extra workbook variants.
  const quote=v=>/[",\r\n]/.test(String(v??''))?'"'+String(v??'').replaceAll('"','""')+'"':String(v??'');
  await fs.writeFile(target,actual.map(r=>r.map(quote).join(',')).join('\r\n')+'\r\n','utf8');
  console.log('CSV_APPLIED',edit.path,matrix.length-1,'rows');
}
await fs.writeFile(path.join(integration,prefix+'CsvEditAudit.json'),JSON.stringify({status:'PASS',renderer:'artifact-tool CSV import and range edit',files:evidence},null,2));
