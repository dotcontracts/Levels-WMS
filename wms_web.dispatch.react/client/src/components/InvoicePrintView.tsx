import React from 'react';
import { Invoice } from '../services/wmsApi';

interface InvoicePrintViewProps {
  invoice: Invoice;
}

export const InvoicePrintView: React.FC<InvoicePrintViewProps> = ({ invoice }) => {
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES'
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-KE');
  };

  return (
    <div className="print-invoice bg-white">
      <style>{`
        @media print {
          .print-invoice {
            width: 210mm;
            margin: 0 auto;
            padding: 20mm;
            font-size: 12pt;
            line-height: 1.4;
            color: black;
          }
          
          .print-header {
            border-bottom: 2px solid #333;
            margin-bottom: 20px;
            padding-bottom: 15px;
          }
          
          .print-company-name {
            font-size: 24pt;
            font-weight: bold;
            color: #333;
          }
          
          .print-invoice-title {
            font-size: 18pt;
            font-weight: bold;
            margin-bottom: 10px;
          }
          
          .print-table {
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
          }
          
          .print-table th,
          .print-table td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
          }
          
          .print-table th {
            background-color: #f5f5f5;
            font-weight: bold;
          }
          
          .print-summary {
            float: right;
            width: 300px;
            margin-top: 20px;
          }
          
          .print-summary-row {
            display: flex;
            justify-content: space-between;
            padding: 5px 0;
            border-bottom: 1px solid #eee;
          }
          
          .print-total-row {
            font-weight: bold;
            font-size: 14pt;
            border-top: 2px solid #333 !important;
            border-bottom: 2px solid #333 !important;
            padding: 10px 0 !important;
          }
          
          .print-footer {
            position: fixed;
            bottom: 10mm;
            width: 100%;
            text-align: center;
            font-size: 10pt;
            color: #666;
          }
        }
        
        @page {
          margin: 15mm;
          size: A4;
        }
      `}</style>

      {/* Header */}
      <div className="print-header">
        <div className="flex justify-between items-start">
          <div>
            <h1 className="print-company-name">FICMA HOME LOGISTICS</h1>
            <p className="text-gray-600">Service beyond customer's expectation</p>
            <p className="text-sm text-gray-500 mt-2">
              Phone: 0712442434 | Email: ficmahomelogistics19@gmail.com<br/>
              Address: Hillocks Godowns, Off Enterprise Road, Godown No 5
            </p>
          </div>
          <div className="text-right">
            <h2 className="print-invoice-title">INVOICE</h2>
            <div className="space-y-1 text-sm">
              <div><strong>Invoice #:</strong> {invoice.invoiceNumber}</div>
              <div><strong>Date:</strong> {formatDate(invoice.issueDate)}</div>
              <div><strong>Due Date:</strong> {formatDate(invoice.dueDate)}</div>
            </div>
          </div>
        </div>
      </div>

      {/* Bill To Section */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mb-6">
        <div>
          <h3 className="font-bold text-lg mb-2">Bill To:</h3>
          <div className="space-y-1">
            <div className="font-semibold">{invoice.customer?.name || 'Unknown Customer'}</div>
            <div>{invoice.customer?.companyName}</div>
            <div>{invoice.customer?.address}</div>
            <div>Email: {invoice.customer?.email}</div>
            <div>Phone: {invoice.customer?.phone}</div>
          </div>
        </div>
        
        <div>
          <h3 className="font-bold text-lg mb-2">Invoice Details:</h3>
          <div className="space-y-1 text-sm">
            <div><strong>Billing Period:</strong></div>
            <div>{formatDate(invoice.billingPeriodStart)} to {formatDate(invoice.billingPeriodEnd)}</div>
            <div className="mt-2"><strong>Payment Terms:</strong> {invoice.customer?.paymentTerms || 'Net 30'}</div>
            <div><strong>Created By:</strong> {invoice.createdBy ? `${invoice.createdBy.firstName} ${invoice.createdBy.lastName}` : 'System'}</div>
          </div>
        </div>
      </div>

      {/* Invoice Items Table */}
      <div className="mb-6">
        <h3 className="font-bold text-lg mb-3">Items & Services</h3>
        <table className="print-table w-full">
          <thead>
            <tr>
              <th className="text-left">Waybill Number</th>
              <th className="text-left">Description</th>
              <th className="text-left">Destination</th>
              <th className="text-center">Created Date</th>
              <th className="text-center">Qty</th>
              <th className="text-right">Total</th>
            </tr>
          </thead>
          <tbody>
            {invoice.items && invoice.items.length > 0 ? (
              invoice.items.map((item, index) => (
                <tr key={index}>
                  <td>{item.waybillNumber}</td>
                  <td>{item.description}</td>
                  <td>{item.destination || 'Not Available'}</td>
                  <td className="text-center">{item.parcelCreatedAt ? formatDate(item.parcelCreatedAt) : 'Not Available'}</td>
                  <td className="text-center">{item.quantity}</td>
                  <td className="text-right font-medium">{formatCurrency(item.totalPrice)}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={6} className="text-center text-gray-500 py-4">
                  No items found for this invoice
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Summary Section */}
      <div className="flex justify-end">
        <div className="print-summary bg-gray-50 p-4 rounded-lg">
          <h3 className="font-bold text-lg mb-3">Invoice Summary</h3>
          
          <div className="print-summary-row">
            <span>Subtotal (excl. VAT):</span>
            <span>{formatCurrency(invoice.subtotal)}</span>
          </div>
          
          <div className="print-summary-row">
            <span>VAT (16%):</span>
            <span>{formatCurrency(invoice.taxAmount)}</span>
          </div>
          
          <div className="print-summary-row print-total-row">
            <span>Total Amount:</span>
            <span>{formatCurrency(invoice.totalAmount)}</span>
          </div>
          
          <div className="print-summary-row">
            <span>Amount Paid:</span>
            <span className="text-green-600">{formatCurrency(invoice.paidAmount)}</span>
          </div>
          
          <div className="print-summary-row">
            <span>Outstanding Balance:</span>
            <span className={invoice.totalAmount - invoice.paidAmount > 0 ? 'text-red-600 font-bold' : 'text-green-600'}>
              {formatCurrency(invoice.totalAmount - invoice.paidAmount)}
            </span>
          </div>
        </div>
      </div>

      {/* Notes Section */}
      {invoice.notes && (
        <div className="mt-6">
          <h3 className="font-bold text-lg mb-2">Notes:</h3>
          <div className="bg-yellow-50 p-3 rounded border-l-4 border-yellow-400">
            <p>{invoice.notes}</p>
          </div>
        </div>
      )}

      {/* Payment Information */}
      <div className="mt-8 border-t pt-6">
        <h3 className="font-bold text-lg mb-3">Payment Information</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-sm">
          <div>
            <h4 className="font-semibold mb-2">Mobile Money Payment Details:</h4>
            <div className="space-y-1">
              <div><strong>Equity Mobile App:</strong> Dial *247#, Business No: 889404, Account: {invoice.invoiceNumber}</div>
              <div><strong>M-PESA/Airtel Money:</strong> Paybill 247247, Account: 889404</div>
              <div><strong>Account Name:</strong> FICMA HOME LOGISTICS</div>
            </div>
          </div>
          
          <div>
            <h4 className="font-semibold mb-2">Payment Instructions:</h4>
            <div className="space-y-1">
              <div>• Payment is due within {invoice.customer?.paymentTerms || 'Net 30'}</div>
              <div>• Equity App: Dial *247#, Business: 889404, Account: {invoice.invoiceNumber}</div>
              <div>• M-PESA/Airtel: Paybill 247247, Account: 889404</div>
            </div>
          </div>
        </div>
      </div>

      {/* Footer */}
      <div className="print-footer">
        <div className="text-center border-t pt-4">
          <p>Thank you for your business!</p>
          <p>FICMA HOME LOGISTICS - Service beyond customer's expectation</p>
          <p>Generated on {new Date().toLocaleDateString('en-KE')} | Invoice #{invoice.invoiceNumber}</p>
        </div>
      </div>
    </div>
  );
};

export const printInvoice = (invoice: Invoice) => {
  const printWindow = window.open('', '_blank');
  if (!printWindow) {
    alert('Please allow popups to print the invoice');
    return;
  }

  const printContent = `
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="UTF-8">
      <title>Invoice ${invoice.invoiceNumber}</title>
      <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
          font-family: Arial, sans-serif;
          font-size: 9pt;
          color: #333;
          line-height: 1.3;
        }

        .invoice-container {
          width: 100%;
          padding: 0;
        }

        /* Fixed footer on every page */
        .page-footer {
          position: fixed;
          bottom: 0;
          left: 0;
          right: 0;
          border-top: 1px solid #ccc;
          padding: 5px 15px;
          background: white;
          font-size: 7.5pt;
          color: #555;
        }

        .page-footer .footer-inner {
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .page-footer .payment-cols {
          display: flex;
          gap: 30px;
        }

        .page-footer .payment-item {
          white-space: nowrap;
        }

        /* Main content gets bottom margin so footer doesn't overlap */
        .main-content {
          margin-bottom: 55px;
        }

        .header {
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          border-bottom: 2px solid #333;
          padding-bottom: 8px;
          margin-bottom: 8px;
        }

        .company-info h1 {
          font-size: 14pt;
          font-weight: bold;
          color: #333;
        }

        .company-info p {
          font-size: 7.5pt;
          color: #555;
          margin-top: 2px;
        }

        .invoice-info {
          text-align: right;
        }

        .invoice-info h2 {
          font-size: 14pt;
          font-weight: bold;
          margin-bottom: 4px;
        }

        .invoice-info div {
          font-size: 8pt;
          line-height: 1.4;
        }

        .billing-section {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 20px;
          margin-bottom: 8px;
          font-size: 8pt;
        }

        .billing-section h3 {
          font-size: 8.5pt;
          font-weight: bold;
          margin-bottom: 3px;
          border-bottom: 1px solid #eee;
          padding-bottom: 2px;
        }

        .billing-section .field { line-height: 1.4; }

        .section-title {
          font-size: 9pt;
          font-weight: bold;
          margin-bottom: 4px;
        }

        .items-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 8pt;
        }

        .items-table th {
          background-color: #f0f0f0;
          font-weight: bold;
          border: 1px solid #ccc;
          padding: 4px 5px;
          text-align: left;
          white-space: nowrap;
        }

        .items-table td {
          border: 1px solid #ddd;
          padding: 3px 5px;
          vertical-align: top;
        }

        .items-table tr:nth-child(even) td { background-color: #fafafa; }

        .text-right { text-align: right; }
        .text-center { text-align: center; }

        .summary-section {
          display: flex;
          justify-content: flex-end;
          margin-top: 8px;
        }

        .summary-table {
          width: 240px;
          font-size: 8pt;
          border-collapse: collapse;
        }

        .summary-table td {
          padding: 3px 5px;
          border-bottom: 1px solid #eee;
        }

        .summary-table .total-row td {
          font-weight: bold;
          border-top: 2px solid #333;
          border-bottom: 2px solid #333;
          font-size: 8.5pt;
          padding: 4px 5px;
        }

        .notes-box {
          margin-top: 8px;
          font-size: 8pt;
          background: #fefce8;
          border-left: 3px solid #facc15;
          padding: 5px 8px;
        }

        @page {
          size: A4;
          margin: 12mm 12mm 22mm 12mm;
        }

        @media print {
          body { print-color-adjust: exact; }
        }
      </style>
    </head>
    <body>
      <div class="invoice-container">

        <!-- Fixed footer (payment info) shown on every page -->
        <div class="page-footer">
          <div class="footer-inner">
            <div class="payment-cols">
              <div class="payment-item"><strong>M-PESA/Airtel:</strong> Paybill 247247 | Acc: 889404 | FICMA HOME LOGISTICS</div>
              <div class="payment-item"><strong>Payment Terms:</strong> ${invoice.customer?.paymentTerms || 'Net 30'} &nbsp;|&nbsp; <strong>Invoice:</strong> ${invoice.invoiceNumber}</div>
            </div>
            <div style="font-size:7pt; color:#888; white-space:nowrap;">Generated: ${new Date().toLocaleDateString('en-KE')}</div>
          </div>
        </div>

        <div class="main-content">
          ${generateInvoiceHTML(invoice)}
        </div>

      </div>
    </body>
    </html>
  `;

  printWindow.document.write(printContent);
  printWindow.document.close();

  printWindow.onload = () => {
    setTimeout(() => {
      printWindow.print();
      printWindow.close();
    }, 250);
  };
};

const generateInvoiceHTML = (invoice: Invoice) => {
  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-KE', { style: 'currency', currency: 'KES' }).format(amount);

  const formatDate = (dateString: string) =>
    new Date(dateString).toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' });

  return `
    <div class="header">
      <div class="company-info">
        <h1>FICMA HOME LOGISTICS</h1>
        <p>Service beyond customer's expectation</p>
        <p>Phone: 0712442434 | Email: ficmahomelogistics19@gmail.com</p>
        <p>Address: Hillocks Godowns, Off Enterprise Road, Godown No 5</p>
      </div>
      <div class="invoice-info">
        <h2>INVOICE</h2>
        <div><strong>Invoice #:</strong> ${invoice.invoiceNumber}</div>
        <div><strong>Date:</strong> ${formatDate(invoice.issueDate)}</div>
        <div><strong>Due Date:</strong> ${formatDate(invoice.dueDate)}</div>
      </div>
    </div>

    <div class="billing-section">
      <div>
        <h3>Bill To</h3>
        <div class="field"><strong>${invoice.customer?.name || 'Unknown Customer'}</strong></div>
        ${invoice.customer?.companyName ? `<div class="field">${invoice.customer.companyName}</div>` : ''}
        ${invoice.customer?.address ? `<div class="field">${invoice.customer.address}</div>` : ''}
        ${invoice.customer?.email ? `<div class="field">Email: ${invoice.customer.email}</div>` : ''}
        ${invoice.customer?.phone ? `<div class="field">Phone: ${invoice.customer.phone}</div>` : ''}
      </div>
      <div>
        <h3>Invoice Details</h3>
        <div class="field"><strong>Billing Period:</strong> ${formatDate(invoice.billingPeriodStart)} – ${formatDate(invoice.billingPeriodEnd)}</div>
        <div class="field"><strong>Payment Terms:</strong> ${invoice.customer?.paymentTerms || 'Net 30'}</div>
        <div class="field"><strong>Created By:</strong> ${invoice.createdBy ? `${invoice.createdBy.firstName} ${invoice.createdBy.lastName}` : 'System'}</div>
      </div>
    </div>

    <div class="section-title">Items &amp; Services</div>
    <table class="items-table">
      <thead>
        <tr>
          <th style="width:11%">Waybill No.</th>
          <th style="width:11%">Destination</th>
          <th style="width:12%">Sender</th>
          <th style="width:12%">Receiver</th>
          <th class="text-center" style="width:6%">Qty</th>
          <th style="width:22%">Description</th>
          <th class="text-right" style="width:11%">Amount</th>
          <th style="width:11%">Clerk</th>
          <th class="text-center" style="width:9%">Date</th>
        </tr>
      </thead>
      <tbody>
        ${invoice.items && invoice.items.length > 0
          ? invoice.items.map(item => `
              <tr>
                <td>${item.waybillNumber}</td>
                <td>${item.destination || '-'}</td>
                <td>${item.sender || '-'}</td>
                <td>${item.receiver || '-'}</td>
                <td class="text-center">${item.quantity}</td>
                <td>${item.description}</td>
                <td class="text-right">${formatCurrency(item.totalPrice)}</td>
                <td>${item.clerkName || '-'}</td>
                <td class="text-center">${item.parcelCreatedAt ? formatDate(item.parcelCreatedAt) : '-'}</td>
              </tr>`).join('')
          : '<tr><td colspan="9" style="text-align:center;padding:8px;color:#666;">No items</td></tr>'
        }
      </tbody>
    </table>

    <div class="summary-section">
      <table class="summary-table">
        <tr><td>Subtotal (excl. VAT)</td><td class="text-right">${formatCurrency(invoice.subtotal)}</td></tr>
        <tr><td>VAT (16%)</td><td class="text-right">${formatCurrency(invoice.taxAmount)}</td></tr>
        <tr class="total-row"><td>Total Amount</td><td class="text-right">${formatCurrency(invoice.totalAmount)}</td></tr>
        <tr><td>Amount Paid</td><td class="text-right" style="color:#059669">${formatCurrency(invoice.paidAmount)}</td></tr>
        <tr><td><strong>Outstanding Balance</strong></td><td class="text-right" style="font-weight:bold;color:${invoice.totalAmount - invoice.paidAmount > 0 ? '#dc2626' : '#059669'}">${formatCurrency(invoice.totalAmount - invoice.paidAmount)}</td></tr>
      </table>
    </div>

    ${invoice.notes ? `<div class="notes-box"><strong>Notes:</strong> ${invoice.notes}</div>` : ''}
  `;
};