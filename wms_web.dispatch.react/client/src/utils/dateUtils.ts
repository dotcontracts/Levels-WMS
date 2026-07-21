const EAT_OFFSET_MS = 3 * 60 * 60 * 1000;

export const toEAT = (dateStr: string | Date): Date =>
  new Date(new Date(dateStr).getTime() + EAT_OFFSET_MS);

export const fmtDate = (dateStr: string | Date): string =>
  toEAT(dateStr).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });

export const fmtTime = (dateStr: string | Date): string =>
  toEAT(dateStr).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });

export const fmtDateTime = (dateStr: string | Date): string =>
  `${fmtDate(dateStr)} ${fmtTime(dateStr)}`;
