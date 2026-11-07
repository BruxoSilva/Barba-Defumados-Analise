'use client';
import { useState, type FormEvent } from 'react';
import { formatCents, parseCents, prepareReceipt, type Receipt, type ReceiptInput } from '@/lib/receipt';

type Row = { id: number; name: string; quantity: string; unitPrice: string };
const blank = (): Row => ({ id: Date.now() + Math.random(), name: '', quantity: '1', unitPrice: '' });

export default function ReceiptApp() {
  const [customerName, setCustomerName] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [rows, setRows] = useState<Row[]>([blank()]);
  const [paymentMethod, setPaymentMethod] = useState<'pix' | 'cash'>('pix');
  const [received, setReceived] = useState('');
  const [issued, setIssued] = useState<Receipt | null>(null);
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);

  const updateRow = (id: number, field: keyof Omit<Row, 'id'>, value: string) => {
    setRows(current => current.map(row => row.id === id ? { ...row, [field]: value } : row));
    setIssued(null);
  };
  let totalCents = 0;
  for (const row of rows) {
    try { totalCents += parseCents(row.unitPrice) * Number(row.quantity || 0); } catch { /* Campo incompleto durante a digitação. */ }
  }
  let changeCents: number | null = null;
  try { if (paymentMethod === 'cash') changeCents = Math.max(0, parseCents(received) - totalCents); } catch { /* Valor ainda incompleto. */ }

  const input: ReceiptInput = {
    customerName, phone, address, paymentMethod, received,
    items: rows.map(({ name, quantity, unitPrice }) => ({ name, quantity: Number(quantity), unitPrice })),
  };

  async function issue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus(''); setIssued(null);
    try { prepareReceipt(input, new Date()); } catch (error) {
      setStatus(error instanceof Error ? error.message : 'Confira os dados do comprovante.');
      return;
    }
    setBusy(true);
    try {
      const response = await fetch('/api/issue', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error || 'Não foi possível imprimir.');
      setIssued(result.receipt);
      setStatus(`Cupom enviado à ELGIN i7(USB) em ${result.receipt.date} às ${result.receipt.time}.`);
    } catch (error) { setStatus(error instanceof Error ? error.message : 'Erro na impressão.'); }
    finally { setBusy(false); }
  }

  function clear() {
    setCustomerName(''); setPhone(''); setAddress(''); setRows([blank()]);
    setPaymentMethod('pix'); setReceived(''); setIssued(null); setStatus('');
  }

  return <main id="conteudo" className="shell">
    <header className="masthead">
      <div className="monogram" aria-hidden="true"><span>BD</span></div>
      <div><p className="eyebrow">Sistema local de comprovantes</p><h1>Barba Defumados <span>BBQ</span></h1><p className="subtitle">Nova emissão de cupom não fiscal</p></div>
    </header>
    <div className="workspace">
      <form onSubmit={issue} className="editor">
        <fieldset className="panel"><legend>Dados do cliente</legend>
          <div className="client-grid">
            <label>Nome do cliente <input name="customerName" autoComplete="name" required maxLength={100} value={customerName} onChange={e => { setCustomerName(e.target.value); setIssued(null); }} /></label>
            <label>Telefone <input name="phone" type="tel" inputMode="tel" autoComplete="tel" required minLength={8} maxLength={30} value={phone} onChange={e => { setPhone(e.target.value); setIssued(null); }} /></label>
            <label className="full">Endereço de entrega / retirada <input name="address" autoComplete="street-address" maxLength={120} value={address} onChange={e => { setAddress(e.target.value); setIssued(null); }} /></label>
          </div>
          <p className="hint">Data e hora serão registradas pelo computador no momento da emissão.</p>
        </fieldset>
        <fieldset className="panel"><legend>Produtos</legend>
          <div className="product-head" aria-hidden="true"><span>Nome do produto</span><span>Qtd.</span><span>Preço unit. (R$)</span><span>Total</span></div>
          {rows.map((row, index) => {
            let line = 0; try { line = parseCents(row.unitPrice) * Number(row.quantity || 0); } catch { /* incompleto */ }
            return <div className="product-row" key={row.id}>
              <label>Produto {index + 1}<input name={`product-${row.id}`} required maxLength={60} value={row.name} onChange={e => updateRow(row.id, 'name', e.target.value)} /></label>
              <label>Quantidade<input name={`quantity-${row.id}`} type="number" min="1" max="999" step="1" required value={row.quantity} onChange={e => updateRow(row.id, 'quantity', e.target.value)} /></label>
              <label>Preço unit. (R$)<input name={`price-${row.id}`} inputMode="decimal" placeholder="0,00" required value={row.unitPrice} onChange={e => updateRow(row.id, 'unitPrice', e.target.value)} /></label>
              <output>{formatCents(line)}</output>
              <button type="button" className="remove" aria-label={`Remover produto ${index + 1}`} disabled={rows.length === 1} onClick={() => { setRows(current => current.filter(r => r.id !== row.id)); setIssued(null); }}>×</button>
            </div>;
          })}
          <button type="button" className="secondary" disabled={rows.length >= 12} onClick={() => { if (rows.length < 12) setRows(current => [...current, blank()]); }}>+ Adicionar produto</button>
        </fieldset>
        <fieldset className="panel"><legend>Pagamento</legend>
          <div className="radios"><label><input name="paymentMethod" type="radio" checked={paymentMethod === 'pix'} onChange={() => { setPaymentMethod('pix'); setIssued(null); }} /> Pix</label><label><input name="paymentMethod" type="radio" checked={paymentMethod === 'cash'} onChange={() => { setPaymentMethod('cash'); setIssued(null); }} /> Dinheiro</label></div>
          {paymentMethod === 'cash' && <div className="cash"><label>Valor recebido (R$)<input name="received" inputMode="decimal" placeholder="0,00" required value={received} onChange={e => { setReceived(e.target.value); setIssued(null); }} /></label><p>Troco: <strong>{changeCents === null ? '—' : formatCents(changeCents)}</strong></p></div>}
        </fieldset>
        <div className="actions"><button className="primary" type="submit" disabled={busy}>{busy ? 'Enviando à impressora...' : 'Emitir cupom não fiscal'}</button><button type="button" className="secondary" onClick={clear}>Limpar tela</button></div>
        <p className="status" role="status" aria-live="polite">{status}</p>
      </form>
      <aside className="summary"><h2>Resumo do pedido</h2><p className="summary-label">Valor total</p><strong className="big-total">{formatCents(totalCents)}</strong><p className="hint">Bobina 80 mm · impressão útil 72 mm</p>
        <div className="receipt-preview" aria-label="Pré-visualização do cupom"><div className="preview-logo">BD</div><div className="preview-brand">BARBA DEFUMADOS<br /><small>B B Q</small></div><h3>CUPOM NÃO FISCAL<br /><small>COMPROVANTE DE PEDIDO</small></h3><p>Cliente: {customerName || '—'}<br />Telefone: {phone || '—'}<br />Data e hora: {issued ? `${issued.date} ${issued.time}` : 'ao emitir'}<br />Endereço: {address || 'Retirada'}</p><b>PRODUTOS</b><div className="preview-items">{rows.map((row, index) => <div key={row.id}>{index + 1}. {row.quantity || '0'} × {row.name || 'Produto'} <span>{row.unitPrice || '0,00'}</span></div>)}</div><p className="preview-total">TOTAL: {formatCents(totalCents)}</p><p>Pagamento: {paymentMethod === 'pix' ? 'Pix' : 'Dinheiro'}{paymentMethod === 'cash' && <> <br />Troco: {changeCents === null ? '—' : formatCents(changeCents)}</>}</p><small>Documento para controle interno. Sem valor fiscal.</small></div>
      </aside>
    </div>
  </main>;
}
