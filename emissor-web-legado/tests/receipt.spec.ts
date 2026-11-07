import { test, expect } from '@playwright/test';

test('preenche pedido, calcula troco e envia a emissão', async ({ page }) => {
  let sent: Record<string, unknown> | null = null;
  await page.route('**/api/issue', async route => {
    sent = route.request().postDataJSON();
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ receipt: { date: '06/11/2026', time: '15:32' } }) });
  });
  await page.goto('/');
  await page.getByRole('textbox', { name: 'Nome do cliente' }).fill('Ana Silva');
  await page.getByRole('textbox', { name: 'Telefone' }).fill('34999999999');
  await page.getByRole('textbox', { name: 'Endereço de entrega / retirada' }).fill('Rua A, 10');
  await page.getByRole('textbox', { name: 'Produto 1' }).fill('Costela BBQ');
  await page.getByRole('spinbutton', { name: 'Quantidade' }).fill('2');
  await page.getByRole('textbox', { name: 'Preço unit. (R$)' }).fill('10,50');
  await page.getByRole('radio', { name: 'Dinheiro' }).check();
  await page.getByRole('textbox', { name: 'Valor recebido (R$)' }).fill('30,00');
  await expect(page.getByText('Troco:', { exact: false }).first()).toContainText('R$ 9,00');
  await page.getByRole('button', { name: 'Emitir cupom não fiscal' }).click();
  await expect(page.locator('p.status')).toContainText('06/11/2026 às 15:32');
  expect(sent).toMatchObject({ customerName: 'Ana Silva', paymentMethod: 'cash', received: '30,00' });
});

test('cabe em tela estreita sem rolagem horizontal', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  const dimensions = await page.evaluate(() => ({ scroll: document.documentElement.scrollWidth, viewport: innerWidth }));
  expect(dimensions.scroll).toBeLessThanOrEqual(dimensions.viewport);
});
