import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BillingFilter,
  Customer,
  CustomPlanOverrides,
  LoginResponse,
  MemorialAdmin,
  MemorialListItem,
  MemorialPayment,
  MemorialStatistics,
  MemorialStatus,
  PagedResult,
  PhotoRef,
  Plan,
  PublicMemorial,
  PublicPlan,
  PublicSiteSettings,
  QrPlateSize,
  SiteSettings,
  TelegramTestResult,
  UpdateSiteSettingsBody
} from '../models/memorial.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password });
  }

  getPublicMemorial(publicId: string): Observable<PublicMemorial> {
    return this.http.get<PublicMemorial>(`/api/public/memorials/${publicId}`);
  }

  recordView(publicId: string, isAdminPreview = false): Observable<void> {
    return this.http.post<void>(`/api/public/memorials/${publicId}/views`, { isAdminPreview });
  }

  listMemorials(params: {
    search?: string;
    status?: MemorialStatus;
    isDemo?: boolean;
    billingFilter?: BillingFilter;
    sortBy?: 'updatedAt' | 'viewCount';
    sortDir?: 'asc' | 'desc';
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<MemorialListItem>> {
    let httpParams = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));
    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }
    if (params.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params.isDemo === true || params.isDemo === false) {
      httpParams = httpParams.set('isDemo', String(params.isDemo));
    }
    if (params.billingFilter) {
      httpParams = httpParams.set('billingFilter', params.billingFilter);
    }
    if (params.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }
    if (params.sortDir) {
      httpParams = httpParams.set('sortDir', params.sortDir);
    }
    return this.http.get<PagedResult<MemorialListItem>>('/api/admin/memorials', { params: httpParams });
  }

  getMemorial(id: string): Observable<MemorialAdmin> {
    return this.http.get<MemorialAdmin>(`/api/admin/memorials/${id}`);
  }

  getAdminPreview(id: string): Observable<PublicMemorial> {
    return this.http.get<PublicMemorial>(`/api/admin/memorials/${id}/preview`);
  }

  createMemorial(body: {
    fullName: string;
    planId: string;
    privacy?: string;
    isDemo?: boolean;
    callsign?: string | null;
    lifePeriod?: string | null;
    shortText?: string | null;
    customOverrides?: CustomPlanOverrides | null;
  }): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>('/api/admin/memorials', {
      fullName: body.fullName,
      planId: body.planId,
      privacy: body.privacy ?? 'Public',
      isDemo: body.isDemo ?? false,
      callsign: body.callsign,
      lifePeriod: body.lifePeriod,
      shortText: body.shortText,
      customOverrides: body.customOverrides ?? undefined
    });
  }

  updateMemorial(
    id: string,
    body: {
      fullName: string;
      privacy: string;
      isDemo?: boolean;
      callsign?: string | null;
      lifePeriod?: string | null;
      shortText?: string | null;
      mainPhotoId?: string | null;
      qrPlateSize?: QrPlateSize | null;
      finalPrice?: number | null;
      isFinalPriceOverridden?: boolean | null;
      customerId?: string | null;
      blocks: { id?: string; type: string; order: number; data: Record<string, unknown> }[];
    }
  ): Observable<MemorialAdmin> {
    return this.http.put<MemorialAdmin>(`/api/admin/memorials/${id}`, body);
  }

  updatePayment(id: string, paymentStatus: 'Unpaid' | 'Paid'): Observable<MemorialAdmin> {
    return this.http.put<MemorialAdmin>(`/api/admin/memorials/${id}/payment`, { paymentStatus });
  }

  confirmInitialPayment(
    id: string,
    body: { amount?: number | null; note?: string | null } = {}
  ): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/billing/confirm-initial`, body);
  }

  confirmRenewalPayment(
    id: string,
    body: { amount?: number | null; note?: string | null } = {}
  ): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/billing/confirm-renewal`, body);
  }

  listMemorialPayments(id: string): Observable<MemorialPayment[]> {
    return this.http.get<MemorialPayment[]>(`/api/admin/memorials/${id}/payments`);
  }

  listCustomers(params: {
    search?: string;
    page?: number;
    pageSize?: number;
  } = {}): Observable<PagedResult<Customer>> {
    let httpParams = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20));
    if (params.search) {
      httpParams = httpParams.set('search', params.search);
    }
    return this.http.get<PagedResult<Customer>>('/api/admin/customers', { params: httpParams });
  }

  getCustomer(id: string): Observable<Customer> {
    return this.http.get<Customer>(`/api/admin/customers/${id}`);
  }

  createCustomer(body: {
    name: string;
    phone: string;
    email?: string | null;
    telegramUsername?: string | null;
    notes?: string | null;
  }): Observable<Customer> {
    return this.http.post<Customer>('/api/admin/customers', body);
  }

  updateCustomer(
    id: string,
    body: {
      name: string;
      phone: string;
      email?: string | null;
      telegramUsername?: string | null;
      notes?: string | null;
    }
  ): Observable<Customer> {
    return this.http.put<Customer>(`/api/admin/customers/${id}`, body);
  }

  assignPlan(
    id: string,
    body: { planId: string; customOverrides?: CustomPlanOverrides | null }
  ): Observable<MemorialAdmin> {
    return this.http.put<MemorialAdmin>(`/api/admin/memorials/${id}/plan`, body);
  }

  adjustUpdates(id: string, delta: 1 | -1): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/updates`, { delta });
  }

  publish(id: string): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/publish`, {});
  }

  archive(id: string): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/archive`, {});
  }

  restore(id: string): Observable<MemorialAdmin> {
    return this.http.post<MemorialAdmin>(`/api/admin/memorials/${id}/restore`, {});
  }

  permanentDelete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/memorials/${id}`);
  }

  reorderBlocks(id: string, blockIds: string[]): Observable<MemorialAdmin> {
    return this.http.put<MemorialAdmin>(`/api/admin/memorials/${id}/blocks/order`, { blockIds });
  }

  uploadPhoto(id: string, file: File, asMainPhoto = false): Observable<PhotoRef> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<PhotoRef>(`/api/admin/memorials/${id}/photos`, form, {
      params: { asMainPhoto: String(asMainPhoto) }
    });
  }

  deletePhoto(id: string, photoId: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/memorials/${id}/photos/${photoId}`);
  }

  downloadQrPng(id: string): Observable<Blob> {
    return this.http.get(`/api/admin/memorials/${id}/qr.png`, { responseType: 'blob' });
  }

  downloadQrSvg(id: string): Observable<Blob> {
    return this.http.get(`/api/admin/memorials/${id}/qr.svg`, { responseType: 'blob' });
  }

  getStatistics(id: string): Observable<MemorialStatistics> {
    return this.http.get<MemorialStatistics>(`/api/admin/memorials/${id}/statistics`);
  }

  listPlans(): Observable<Plan[]> {
    return this.http.get<Plan[]>('/api/admin/plans');
  }

  updatePlan(
    id: string,
    body: {
      name: string;
      description?: string | null;
      price?: number;
      initialPrice: number;
      renewalPrice: number;
      isActive: boolean;
      isUnlimited: boolean;
      maxBlocks?: number | null;
      maxGalleryBlocks?: number | null;
      maxPhotosPerGallery?: number | null;
      maxTimelineEvents?: number | null;
      maxMemories?: number | null;
      includedUpdates: number;
    }
  ): Observable<Plan> {
    return this.http.put<Plan>(`/api/admin/plans/${id}`, body);
  }

  getPublicPlans(): Observable<PublicPlan[]> {
    return this.http.get<PublicPlan[]>('/api/public/plans');
  }

  getPublicSettings(): Observable<PublicSiteSettings> {
    return this.http.get<PublicSiteSettings>('/api/public/settings');
  }

  getAdminSettings(): Observable<SiteSettings> {
    return this.http.get<SiteSettings>('/api/admin/settings');
  }

  updateAdminSettings(body: UpdateSiteSettingsBody): Observable<SiteSettings> {
    return this.http.put<SiteSettings>('/api/admin/settings', body);
  }

  uploadHomeImage(file: File): Observable<PhotoRef> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<PhotoRef>('/api/admin/settings/home-images', form);
  }

  testTelegramNotify(): Observable<TelegramTestResult> {
    return this.http.post<TelegramTestResult>('/api/admin/settings/test-telegram', {});
  }
}
