import {test,expect} from '@playwright/test';

test('painel aceita e exibe valores em reais',async({page})=>{
 await page.goto('/admin/login');
 await page.getByLabel('E-mail').fill(process.env.ADMIN_EMAIL!);
 await page.getByLabel('Senha').fill(process.env.ADMIN_PASSWORD!);
 await page.getByRole('button',{name:'Entrar'}).click();
 await expect(page).toHaveURL(/\/admin$/);
 await page.goto('/admin/configuracoes');
 const setting=page.locator('form').filter({has:page.locator('input[name="key"][value="shipping.defaultCents"]')});
 await expect(setting.getByLabel(/Frete padrão/)).toHaveValue('15,00');
 await setting.getByLabel(/Frete padrão/).fill('12,34');
 await setting.getByRole('button',{name:'Salvar'}).click();
 await expect(page).toHaveURL(/salvo=1/);
 await expect(page.locator('form').filter({has:page.locator('input[name="key"][value="shipping.defaultCents"]')}).getByLabel(/Frete padrão/)).toHaveValue('12,34');
 await page.goto('/admin/campanhas');
 const rate=page.locator('form').filter({has:page.getByRole('heading',{name:'Nova taxa'})});
 await rate.getByLabel('Nome').fill('Taxa teste');
 await rate.getByLabel('Taxa (R$)').fill('7,50');
 await rate.getByRole('button',{name:'Salvar taxa'}).click();
 await expect(page.getByRole('heading',{name:'Taxa teste'})).toBeVisible();
 await expect(page.locator('form').filter({has:page.getByRole('heading',{name:'Taxa teste'})}).getByLabel('Taxa (R$)')).toHaveValue('7,50');
 const csv=await page.request.get('/api/admin/export/vendas');
 expect(csv.ok()).toBeTruthy();
 expect(await csv.text()).toContain('Subtotal (R$)');
});
