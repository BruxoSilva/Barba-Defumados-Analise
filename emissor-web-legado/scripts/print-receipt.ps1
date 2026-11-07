$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
$script:receipt = [Console]::In.ReadToEnd() | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing

$printer = 'ELGIN i7(USB)'
$doc = [System.Drawing.Printing.PrintDocument]::new()
$doc.PrinterSettings.PrinterName = $printer
if (-not $doc.PrinterSettings.IsValid) { throw "A impressora $printer nao esta instalada." }
$doc.DocumentName = "Cupom nao fiscal - $($script:receipt.customerName)"
$doc.OriginAtMargins = $false
$doc.DefaultPageSettings.Margins = [System.Drawing.Printing.Margins]::new(0,0,0,0)
$doc.DefaultPageSettings.PaperSize = [System.Drawing.Printing.PaperSize]::new('Cupom 72 mm', 283, 1169)

$handler = [System.Drawing.Printing.PrintPageEventHandler]{
  param($sender, $e)
  $g = $e.Graphics
  $g.PageUnit = [System.Drawing.GraphicsUnit]::Millimeter
  $brush = [System.Drawing.Brushes]::Black
  $font = [System.Drawing.Font]::new('Arial', 8)
  $bold = [System.Drawing.Font]::new('Arial', 8, [System.Drawing.FontStyle]::Bold)
  $small = [System.Drawing.Font]::new('Arial', 7)
  $title = [System.Drawing.Font]::new('Arial', 11, [System.Drawing.FontStyle]::Bold)
  $brand = [System.Drawing.Font]::new('Georgia', 13, [System.Drawing.FontStyle]::Bold)
  $line = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 0.25)
  $center = [System.Drawing.StringFormat]::new()
  $center.Alignment = [System.Drawing.StringAlignment]::Center
  $right = [System.Drawing.StringFormat]::new()
  $right.Alignment = [System.Drawing.StringAlignment]::Far
  try {
    $g.DrawEllipse($line, 26, 2, 20, 20)
    $g.DrawString('BD', $brand, $brush, [System.Drawing.RectangleF]::new(26, 6, 20, 11), $center)
    $g.DrawString('BARBA DEFUMADOS', $brand, $brush, [System.Drawing.RectangleF]::new(1, 24, 70, 9), $center)
    $g.DrawString('B B Q', $small, $brush, [System.Drawing.RectangleF]::new(1, 32, 70, 5), $center)
    $g.DrawString('CUPOM NÃO FISCAL', $title, $brush, [System.Drawing.RectangleF]::new(1, 39, 70, 7), $center)
    $g.DrawString('COMPROVANTE DE PEDIDO', $bold, $brush, [System.Drawing.RectangleF]::new(1, 46, 70, 6), $center)
    $y = 54
    $g.DrawString("Cliente: $($script:receipt.customerName)", $font, $brush, [System.Drawing.RectangleF]::new(1, $y, 70, 11))
    $y += 9
    $g.DrawString("Telefone: $($script:receipt.phone)", $font, $brush, 1, $y)
    $y += 6
    $g.DrawString("Data: $($script:receipt.date)    Hora: $($script:receipt.time)", $font, $brush, 1, $y)
    $y += 6
    $addr = if ($script:receipt.address) { $script:receipt.address } else { 'Retirada' }
    $g.DrawString("Endereço: $addr", $font, $brush, [System.Drawing.RectangleF]::new(1, $y, 70, 13))
    $y += 13
    $g.DrawLine($line, 1, $y, 71, $y)
    $y += 2
    $g.DrawString('QTD.  PRODUTO', $bold, $brush, 1, $y)
    $g.DrawString('TOTAL', $bold, $brush, [System.Drawing.RectangleF]::new(52, $y, 19, 6), $right)
    $y += 6
    foreach ($item in $script:receipt.items) {
      $name = "$($item.quantity) UN  $($item.name)"
      $g.DrawString($name, $font, $brush, [System.Drawing.RectangleF]::new(1, $y, 50, 12))
      $amount = ('{0:N2}' -f ($item.totalCents / 100))
      $g.DrawString($amount, $font, $brush, [System.Drawing.RectangleF]::new(52, $y, 19, 7), $right)
      $y += 12
    }
    $g.DrawLine($line, 1, $y, 71, $y)
    $y += 3
    $total = ('R$ {0:N2}' -f ($script:receipt.totalCents / 100))
    $g.DrawString("VALOR TOTAL: $total", $title, $brush, [System.Drawing.RectangleF]::new(1, $y, 70, 9), $center)
    $y += 10
    $method = if ($script:receipt.paymentMethod -eq 'pix') { 'Pix' } else { 'Dinheiro' }
    $g.DrawString("Pagamento: $method", $font, $brush, 1, $y)
    $y += 6
    if ($method -eq 'Dinheiro') {
      $received = ('R$ {0:N2}' -f ($script:receipt.receivedCents / 100))
      $change = ('R$ {0:N2}' -f ($script:receipt.changeCents / 100))
      $g.DrawString("Recebido: $received    Troco: $change", $font, $brush, 1, $y)
      $y += 7
    }
    $y += 4
    $g.DrawString('Documento para controle interno. Não substitui', $small, $brush, [System.Drawing.RectangleF]::new(1, $y, 70, 6), $center)
    $y += 5
    $g.DrawString('a nota fiscal. Sem valor fiscal.', $small, $brush, [System.Drawing.RectangleF]::new(1, $y, 70, 6), $center)
    $e.HasMorePages = $false
  } finally {
    $font.Dispose(); $bold.Dispose(); $small.Dispose(); $title.Dispose(); $brand.Dispose(); $line.Dispose(); $center.Dispose(); $right.Dispose()
  }
}
$doc.add_PrintPage($handler)
try { $doc.Print() }
finally { $doc.remove_PrintPage($handler); $doc.Dispose() }
