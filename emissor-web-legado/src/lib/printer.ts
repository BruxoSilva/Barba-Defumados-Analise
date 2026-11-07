import { spawn } from 'node:child_process';
import path from 'node:path';
import type { Receipt } from './receipt';

export function printReceipt(receipt: Receipt): Promise<void> {
  if (process.platform !== 'win32') return Promise.reject(new Error('A impressão exige Windows.'));
  const script = path.join(process.cwd(), 'scripts', 'print-receipt.ps1');
  return new Promise((resolve, reject) => {
    const child = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', script], {
      cwd: process.cwd(), windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'],
    });
    let stderr = '';
    child.stderr.setEncoding('utf8');
    child.stderr.on('data', chunk => { stderr += chunk; });
    child.on('error', reject);
    child.on('close', code => code === 0 ? resolve() : reject(new Error(stderr.trim() || 'Falha ao enviar à impressora.')));
    child.stdin.end(JSON.stringify(receipt), 'utf8');
  });
}
