'use server';import { redirect } from 'next/navigation';import { requireAdmin } from '@/lib/session';import { backupDatabase } from '@/lib/jobs';
export async function manualBackupAction(){await requireAdmin();await backupDatabase();redirect('/admin?backup=1')}
