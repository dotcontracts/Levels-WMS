import React, { useState } from 'react';
import { DollarSign, Calendar, CreditCard, AlertCircle } from 'lucide-react';
import { Modal, Button, Input, Select } from './ui';
import { Invoice } from '../services/wmsApi';

interface PaymentModalProps {
  invoice: Invoice | null;
  isOpen: boolean;
  onClose: () => void;
  onPaymentRecord: (paymentData: PaymentData) => Promise<void>;
}

interface PaymentData {
  invoiceId: number;
  amount: number;
  paymentMethod: 'cash' | 'cheque' | 'bank_transfer' | 'mobile_money';
  paymentDate: string;
  paymentReference?: string;
  notes?: string;
}

export const PaymentModal: React.FC<PaymentModalProps> = ({
  invoice,
  isOpen,
  onClose,
  onPaymentRecord
}) => {
  const [formData, setFormData] = useState({
    amount: '',
    paymentMethod: 'bank_transfer' as PaymentData['paymentMethod'],
    paymentDate: new Date().toISOString().split('T')[0],
    paymentReference: '',
    notes: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-KE', {
      style: 'currency',
      currency: 'KES'
    }).format(amount);
  };

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.amount || parseFloat(formData.amount) <= 0) {
      newErrors.amount = 'Amount must be greater than 0';
    }

    if (invoice && parseFloat(formData.amount) > (invoice.totalAmount - invoice.paidAmount)) {
      newErrors.amount = 'Amount cannot exceed outstanding balance';
    }

    if (!formData.paymentDate) {
      newErrors.paymentDate = 'Payment date is required';
    }

    if (!formData.paymentMethod) {
      newErrors.paymentMethod = 'Payment method is required';
    }

    if (formData.paymentMethod === 'cheque' && !formData.paymentReference) {
      newErrors.paymentReference = 'Cheque number is required';
    }

    if (formData.paymentMethod === 'bank_transfer' && !formData.paymentReference) {
      newErrors.paymentReference = 'Transaction reference is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!invoice || !validateForm()) {
      return;
    }

    try {
      setIsSubmitting(true);

      const paymentData: PaymentData = {
        invoiceId: invoice.id,
        amount: parseFloat(formData.amount),
        paymentMethod: formData.paymentMethod,
        paymentDate: formData.paymentDate,
        paymentReference: formData.paymentReference,
        notes: formData.notes
      };

      await onPaymentRecord(paymentData);

      // Reset form
      setFormData({
        amount: '',
        paymentMethod: 'bank_transfer',
        paymentDate: new Date().toISOString().split('T')[0],
        paymentReference: '',
        notes: ''
      });

      onClose();
    } catch (error) {
      setErrors({ submit: error instanceof Error ? error.message : 'Failed to record payment' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    setFormData({
      amount: '',
      paymentMethod: 'bank_transfer',
      paymentDate: new Date().toISOString().split('T')[0],
      paymentReference: '',
      notes: ''
    });
    setErrors({});
    onClose();
  };

  if (!invoice) return null;

  const outstandingAmount = invoice.totalAmount - invoice.paidAmount;

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Record Payment"
      icon={<DollarSign className="w-5 h-5" />}
      size="md"
    >
      <div className="w-full">
        {/* Invoice Summary */}
        <div className="bg-gray-50 p-4 rounded-lg mb-6">
          <h3 className="font-semibold text-gray-900 mb-2">Invoice Details</h3>
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span>Invoice #:</span>
              <span className="font-medium">{invoice.invoiceNumber}</span>
            </div>
            <div className="flex justify-between">
              <span>Customer:</span>
              <span className="font-medium">{invoice.customer?.name}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Amount:</span>
              <span className="font-medium">{formatCurrency(invoice.totalAmount)}</span>
            </div>
            <div className="flex justify-between">
              <span>Paid Amount:</span>
              <span className="font-medium text-green-600">{formatCurrency(invoice.paidAmount)}</span>
            </div>
            <div className="flex justify-between border-t pt-2">
              <span className="font-semibold">Outstanding:</span>
              <span className="font-bold text-red-600">{formatCurrency(outstandingAmount)}</span>
            </div>
          </div>
        </div>

        {/* Payment Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Amount */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <DollarSign className="w-4 h-4 inline mr-1" />
              Payment Amount *
            </label>
            <Input
              type="number"
              step="0.01"
              min="0"
              max={outstandingAmount}
              value={formData.amount}
              onChange={(e) => handleChange('amount', e.target.value)}
              placeholder="0.00"
            />
            {errors.amount && (
              <p className="text-sm text-red-600 mt-1">{errors.amount}</p>
            )}
            <p className="text-xs text-gray-500 mt-1">
              Maximum: {formatCurrency(outstandingAmount)}
            </p>
          </div>

          {/* Payment Method */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <CreditCard className="w-4 h-4 inline mr-1" />
              Payment Method *
            </label>
            <Select
              value={formData.paymentMethod}
              onChange={(value) => handleChange('paymentMethod', String(value))}
              options={[
                { value: 'bank_transfer', label: 'Bank Transfer' },
                { value: 'cheque', label: 'Cheque' },
                { value: 'cash', label: 'Cash' },
                { value: 'mobile_money', label: 'Mobile Money' }
              ]}
            />
            {errors.paymentMethod && (
              <p className="text-sm text-red-600 mt-1">{errors.paymentMethod}</p>
            )}
          </div>

          {/* Payment Date */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Calendar className="w-4 h-4 inline mr-1" />
              Payment Date *
            </label>
            <Input
              type="date"
              value={formData.paymentDate}
              onChange={(e) => handleChange('paymentDate', e.target.value)}
            />
            {errors.paymentDate && (
              <p className="text-sm text-red-600 mt-1">{errors.paymentDate}</p>
            )}
          </div>

          {/* Payment Reference */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              {formData.paymentMethod === 'cheque' ? 'Cheque Number' :
                formData.paymentMethod === 'bank_transfer' ? 'Transaction Reference' :
                  formData.paymentMethod === 'mobile_money' ? 'Transaction ID' :
                    'Payment Reference'}
              {(formData.paymentMethod === 'cheque' || formData.paymentMethod === 'bank_transfer') && ' *'}
            </label>
            <Input
              type="text"
              value={formData.paymentReference}
              onChange={(e) => handleChange('paymentReference', e.target.value)}
              placeholder={
                formData.paymentMethod === 'cheque' ? 'Enter cheque number' :
                  formData.paymentMethod === 'bank_transfer' ? 'Enter transaction reference' :
                    formData.paymentMethod === 'mobile_money' ? 'Enter M-Pesa/Airtel code' :
                      'Enter reference (optional)'
              }
            />
            {errors.paymentReference && (
              <p className="text-sm text-red-600 mt-1">{errors.paymentReference}</p>
            )}
          </div>

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Notes (Optional)
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => handleChange('notes', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              rows={3}
              placeholder="Additional notes about this payment..."
            />
          </div>

          {/* Submit Error */}
          {errors.submit && (
            <div className="flex items-center p-3 bg-red-50 border border-red-200 rounded-md">
              <AlertCircle className="w-5 h-5 text-red-500 mr-2" />
              <p className="text-sm text-red-600">{errors.submit}</p>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex justify-end space-x-3 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              isLoading={isSubmitting}
            >
              Record Payment
            </Button>
          </div>
        </form>

        {/* Quick Amount Buttons */}
        {outstandingAmount > 0 && (
          <div className="mt-4 pt-4 border-t">
            <p className="text-sm font-medium text-gray-700 mb-2">Quick amounts:</p>
            <div className="flex space-x-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => handleChange('amount', outstandingAmount.toString())}
                disabled={isSubmitting}
              >
                Full Amount
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => handleChange('amount', (outstandingAmount / 2).toString())}
                disabled={isSubmitting}
              >
                Half Amount
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};