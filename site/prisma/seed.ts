import { PrismaClient } from '@prisma/client';
import { hash } from 'bcryptjs';
const db = new PrismaClient();
async function main() {
 const email=process.env.ADMIN_EMAIL?.trim().toLowerCase();
 const password=process.env.ADMIN_PASSWORD;
 if (!email || !password || password.length < 8) throw new Error('Defina ADMIN_EMAIL e ADMIN_PASSWORD (mínimo 8 caracteres) no .env');
 await db.user.upsert({where:{email},update:{name:'Administrador',role:'ADMIN',passwordHash:await hash(password,12)},create:{email,name:'Administrador',role:'ADMIN',passwordHash:await hash(password,12)}});
 const settings:[string,string,string,string,string][]=[
  ['store.name','Barba Defumados','string','brand','Nome da loja'],
  ['store.subtitle','Charcutaria em Geral','string','brand','Subtítulo'],
  ['store.heroTitle','O sabor do tempo, da brasa e do cuidado.','string','brand','Título principal'],
  ['store.heroText','Charcutaria artesanal e defumados feitos para transformar encontros em momentos memoráveis.','string','brand','Apresentação'],
  ['store.heroCta','Explorar sabores','string','brand','Texto do botão principal'],
  ['store.categoriesTitle','Da nossa casa para a sua mesa','string','brand','Título de categorias'],
  ['store.productsTitle','Escolhas da casa','string','brand','Título de produtos'],
  ['store.footerText','Feito com fogo, tempo e intenção.','string','brand','Texto do rodapé'],
  ['store.navCategories','Categorias','string','brand','Menu de categorias'],
  ['store.navProducts','Produtos','string','brand','Menu de produtos'],
  ['store.navCep','Consultar CEP','string','brand','Menu de CEP'],
  ['store.navAdmin','Área administrativa','string','brand','Menu administrativo'],
  ['store.heroEyebrow','Charcutaria artesanal','string','brand','Chamada do destaque'],
  ['store.categoriesEyebrow','Explore','string','brand','Chamada de categorias'],
  ['store.productsEyebrow','Seleção especial','string','brand','Chamada de produtos'],
  ['store.noticeEyebrow','Em breve','string','brand','Chamada inferior'],
  ['store.noticeTitle','Mais sabor a cada encontro.','string','brand','Título inferior'],
  ['store.noticeCta','Ver categorias','string','brand','Botão inferior'],
  ['store.feedbackLabel','Feedback','string','brand','Botão de feedback'],
  ['store.feedbackPrompt','Conte o que achou da loja','string','brand','Pergunta de feedback'],
  ['brand.copper','#ac643e','color','brand','Cor cobre'],
  ['brand.forest','#263227','color','brand','Cor carvão/verde'],
  ['brand.cream','#f6f0e4','color','brand','Cor creme'],
  ['brand.paper','#fbf8f1','color','brand','Cor de fundo'],
  ['store.logoPath','','string','brand','Logo local'],
  ['store.heroImagePath','','string','brand','Imagem de destaque local'],
  ['cart.reservationMinutes','360','integer','cart','Reserva do carrinho (minutos)'],
  ['cart.warningMinutes','60','integer','cart','Aviso antes de expirar (minutos)'],
  ['reviews.windowHours','48','integer','reviews','Janela de avaliação (horas)'],
  ['referrals.rankingEnabled','false','boolean','referrals','Exibir ranking'],
  ['customer.ageConfirmationEnabled','false','boolean','customer','Confirmar maioridade']
  ,['shipping.minimumOrderCents','0','integer','shipping','Pedido mínimo (R$)']
  ,['shipping.freeAboveCents','15000','integer','shipping','Frete grátis acima de (R$)']
  ,['shipping.defaultCents','1500','integer','shipping','Frete padrão (R$)']
  ,['shipping.pickupEnabled','true','boolean','shipping','Permitir retirada']
  ,['shipping.deliverySlots','Terça 14:00-18:00\nQuinta 14:00-18:00','string','shipping','Janelas de entrega']
  ,['cashback.defaultPercentBps','500','integer','cashback','Cashback padrão em pontos base']
  ,['cashback.validDays','90','integer','cashback','Validade do cashback em dias']
  ,['cashback.warningDays','7','integer','cashback','Avisar vencimento em dias']
  ,['referrals.blockSamePhone','true','boolean','referrals','Bloquear mesmo telefone']
  ,['referrals.blockSameAddress','true','boolean','referrals','Bloquear mesmo endereço']
  ,['referrals.buyerDiscountBps','0','integer','referrals','Desconto de quem usa convite']
  ,['referrals.percentDateBasis','purchase','string','referrals','Base da porcentagem: purchase ou creation']
  ,['store.terms','Termos de Uso provisórios.','string','legal','Termos de Uso']
  ,['store.privacy','Política de Privacidade provisória.','string','legal','Política de Privacidade']
  ,['messages.orderStatus','Seu pedido agora está em: {status}.','string','messages','Status do pedido']
  ,['messages.cartWarning','Se não finalizar a compra, seu carrinho será esvaziado.','string','messages','Aviso do carrinho']
  ,['messages.reviewAvailable','Sua avaliação já está disponível! Se já consumiu, conte o que achou. A avaliação fica aberta por 48 horas.','string','messages','Avaliação disponível']
  ,['messages.cashbackExpiry','Seu saldo de cashback está perto de vencer.','string','messages','Cashback a vencer']
  ,['messages.backInStock','O produto que você queria voltou ao estoque.','string','messages','Produto disponível']
 ];
 for(const [key,value,type,group,label] of settings) await db.setting.upsert({where:{key},update:{},create:{key,value,type,group,label}});
 const categories=[['Defumados','defumados'],['Charcutaria','charcutaria'],['Kits & Tábuas','kits-tabuas']];
 for(const [position,[name,slug]] of categories.entries()) await db.category.upsert({where:{slug},update:{},create:{name,slug,position}});
 const products=[
  ['Pancetta Defumada','pancetta-defumada','Pancetta de cura lenta, com notas suaves de madeira e final marcante.',4590,'defumados'],
  ['Linguiça Artesanal','linguica-artesanal','Temperos equilibrados e defumação cuidadosa para servir em qualquer ocasião.',3290,'charcutaria'],
  ['Tábua da Casa','tabua-da-casa','Uma seleção de sabores para compartilhar com quem importa.',8990,'kits-tabuas']
 ] as const;
 for(const [name,slug,description,priceCents,categorySlug] of products) {
  const category=await db.category.findUniqueOrThrow({where:{slug:categorySlug}});
  await db.product.upsert({where:{slug},update:{},create:{name,slug,description,priceCents,categoryId:category.id}});
  const product=await db.product.findUniqueOrThrow({where:{slug}});
  await db.stockLot.upsert({where:{productId_code:{productId:product.id,code:'LOTE-TESTE'}},update:{},create:{productId:product.id,code:'LOTE-TESTE',quantityUnits:20,expiresAt:new Date('2027-12-31T23:59:59Z')}});
 }
 for (const [email,name] of [['cliente1@teste.local','Cliente de Teste 1'],['cliente2@teste.local','Cliente de Teste 2']]) await db.user.upsert({where:{email},update:{},create:{email,name}});
}
main().finally(()=>db.$disconnect());
