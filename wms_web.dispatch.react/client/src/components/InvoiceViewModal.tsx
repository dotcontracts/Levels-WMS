import React from 'react';
import { X, FileText, Building2, Calendar, DollarSign, Package, User } from 'lucide-react';
import { Modal, Button, Badge } from './ui';
import { Invoice } from '../services/wmsApi';

interface InvoiceViewModalProps {
  invoice: Invoice | null;
  isOpen: boolean;
  onClose: () => void;
  onPrint: () => void;
}

export const InvoiceViewModal: React.FC<InvoiceViewModalProps> = ({
  invoice,
  isOpen,
  onClose,
  onPrint
}) => {
  if (!invoice) return null;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES'
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-KE');
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: 'gray' | 'blue' | 'green' | 'red' | 'yellow', label: string }> = {
      draft: { color: 'gray', label: 'Draft' },
      sent: { color: 'blue', label: 'Sent' },
      paid: { color: 'green', label: 'Paid' },
      overdue: { color: 'red', label: 'Overdue' },
      cancelled: { color: 'red', label: 'Cancelled' }
    };

    const config = statusConfig[status] || { color: 'gray', label: status };

    return (
      <Badge color={config.color}>
        {config.label}
      </Badge>
    );
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={`Invoice ${invoice.invoiceNumber}`}
      icon={<FileText className="w-5 h-5" />}
      size="xl"
    >
      <div className="w-full">
        {/* Header */}
        <div className="flex items-center justify-between mb-6 pb-4 border-b border-gray-200 dark:border-gray-700">
          <div className="flex flex-col">
            <h2 className="text-xl font-bold text-gray-900 dark:text-white">Invoice Details</h2>
          </div>
          <div className="flex items-center space-x-3">
            {getStatusBadge(invoice.status)}
            <Button
              variant="outline"
              onClick={onPrint}
              className="flex items-center space-x-2"
            >
              <FileText className="w-4 h-4" />
              <span>Print</span>
            </Button>
          </div>
        </div>

        {/* Invoice Information Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
          {/* Customer Information */}
          <div className="bg-gray-50 p-4 rounded-lg">
            <h3 className="flex items-center text-lg font-semibold text-gray-900 mb-3">
              <Building2 className="w-5 h-5 mr-2" />
              Customer Details
            </h3>
            <div className="space-y-2 text-sm">
              <div><strong>Name:</strong> {invoice.customer?.name || 'Unknown'}</div>
              <div><strong>Company:</strong> {invoice.customer?.companyName || 'N/A'}</div>
              <div><strong>Email:</strong> {invoice.customer?.email || 'N/A'}</div>
              <div><strong>Phone:</strong> {invoice.customer?.phone || 'N/A'}</div>
              <div><strong>Address:</strong> {invoice.customer?.address || 'N/A'}</div>
            </div>
          </div>

          {/* Invoice Information */}
          <div className="bg-gray-50 p-4 rounded-lg">
            <h3 className="flex items-center text-lg font-semibold text-gray-900 mb-3">
              <Calendar className="w-5 h-5 mr-2" />
              Invoice Information
            </h3>
            <div className="space-y-2 text-sm">
              <div><strong>Issue Date:</strong> {formatDate(invoice.issueDate)}</div>
              <div><strong>Due Date:</strong> {formatDate(invoice.dueDate)}</div>
              <div><strong>Billing Period:</strong> {formatDate(invoice.billingPeriodStart)} - {formatDate(invoice.billingPeriodEnd)}</div>
              <div><strong>Created By:</strong> {invoice.createdBy ? `${invoice.createdBy.firstName} ${invoice.createdBy.lastName}` : 'Unknown'}</div>
              <div><strong>Created At:</strong> {formatDate(invoice.createdAt)}</div>
            </div>
          </div>
        </div>

        {/* Invoice Items */}
        <div className="mb-6">
          <h3 className="flex items-center text-lg font-semibold text-gray-900 mb-4">
            <Package className="w-5 h-5 mr-2" />
            Invoice Items ({invoice.items?.length || 0})
          </h3>

          {invoice.items && invoice.items.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full border border-gray-200 rounded-lg">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Waybill</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Destination</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Created Date</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Qty</th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {invoice.items.map((item, index) => (
                    <tr key={index} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-sm font-medium text-gray-900">{item.waybillNumber}</td>
                      <td className="px-4 py-3 text-sm text-gray-900">{item.description}</td>
                      <td className="px-4 py-3 text-sm text-gray-900">{item.destination || 'Not Available'}</td>
                      <td className="px-4 py-3 text-sm text-center text-gray-900">{item.parcelCreatedAt ? formatDate(item.parcelCreatedAt) : 'Not Available'}</td>
                      <td className="px-4 py-3 text-sm text-center text-gray-900">{item.quantity}</td>
                      <td className="px-4 py-3 text-sm text-right font-medium text-gray-900">{formatCurrency(item.totalPrice)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">
              <Package className="w-12 h-12 mx-auto mb-2" />
              <p>No invoice items found</p>
            </div>
          )}
        </div>

        {/* Financial Summary */}
        <div className="bg-gray-50 p-6 rounded-lg">
          <h3 className="flex items-center text-lg font-semibold text-gray-900 mb-4">
            <DollarSign className="w-5 h-5 mr-2" />
            Financial Summary
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-gray-600">Subtotal:</span>
                <span className="font-medium">{formatCurrency(invoice.subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">VAT (16%):</span>
                <span className="font-medium">{formatCurrency(invoice.taxAmount)}</span>
              </div>
              <div className="flex justify-between border-t pt-3">
                <span className="text-lg font-semibold text-gray-900">Total Amount:</span>
                <span className="text-lg font-bold text-gray-900">{formatCurrency(invoice.totalAmount)}</span>
              </div>
            </div>

            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-gray-600">Amount Paid:</span>
                <span className="font-medium text-green-600">{formatCurrency(invoice.paidAmount)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Outstanding:</span>
                <span className="font-medium text-red-600">{formatCurrency(invoice.totalAmount - invoice.paidAmount)}</span>
              </div>
              <div className="flex justify-between border-t pt-3">
                <span className="text-lg font-semibold text-gray-900">Payment Status:</span>
                <div>{getStatusBadge(invoice.status)}</div>
              </div>
            </div>
          </div>
        </div>

        {/* Notes */}
        {invoice.notes && (
          <div className="mt-6 bg-yellow-50 p-4 rounded-lg">
            <h4 className="font-semibold text-gray-900 mb-2">Notes:</h4>
            <p className="text-gray-700">{invoice.notes}</p>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex justify-end space-x-3 mt-6 pt-4 border-t">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
          <Button onClick={onPrint} className="flex items-center space-x-2">
            <FileText className="w-4 h-4" />
            <span>Print Invoice</span>
          </Button>
        </div>
      </div>
    </Modal>
  );
};