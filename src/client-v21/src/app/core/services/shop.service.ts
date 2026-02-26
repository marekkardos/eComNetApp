import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Product, Brand, ProductType } from '../../shared/models/product.model';
import { Pagination, ShopParams } from '../../shared/models/pagination.model';

@Injectable({ providedIn: 'root' })
export class ShopService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  products: Product[] = [];
  brands: Brand[] = [];
  types: ProductType[] = [];
  pagination = signal<Pagination<Product>>({
    pageIndex: 1,
    pageSize: 6,
    count: 0,
    data: []
  });
  shopParams = signal<ShopParams>({
    brandId: 0,
    typeId: 0,
    sort: 'name',
    pageNumber: 1,
    pageSize: 6,
    search: ''
  });

  getProducts(useCache: boolean): Observable<Pagination<Product>> {
    const currentParams = this.shopParams();

    if (useCache === false) {
      this.products = [];
    }

    if (this.products.length > 0 && useCache === true) {
      const pagesReceived = Math.ceil(this.products.length / currentParams.pageSize);

      if (currentParams.pageNumber <= pagesReceived) {
        const pagination = this.pagination();
        pagination.data = this.products.slice(
          (currentParams.pageNumber - 1) * currentParams.pageSize,
          currentParams.pageNumber * currentParams.pageSize
        );
        return of(pagination);
      }
    }

    let params = new HttpParams();

    if (currentParams.brandId !== 0) {
      params = params.append('brandId', currentParams.brandId.toString());
    }

    if (currentParams.typeId !== 0) {
      params = params.append('typeId', currentParams.typeId.toString());
    }

    if (currentParams.search) {
      params = params.append('search', currentParams.search);
    }

    params = params.append('sort', currentParams.sort);
    params = params.append('pageIndex', currentParams.pageNumber.toString());
    params = params.append('pageSize', currentParams.pageSize.toString());

    return this.http.get<Pagination<Product>>(`${this.baseUrl}products`, {
      observe: 'response',
      params
    }).pipe(
      map(response => {
        if (response.body) {
          this.products = [...this.products, ...response.body.data];
          this.pagination.set(response.body);
          return response.body;
        }
        return {
          pageIndex: currentParams.pageNumber,
          pageSize: currentParams.pageSize,
          count: 0,
          data: []
        };
      })
    );
  }

  getNewArrivals(count: number = 4): Observable<Product[]> {
    const params = new HttpParams()
      .set('sort', 'newest')
      .set('pageSize', count.toString())
      .set('pageIndex', '1');

    return this.http.get<Pagination<Product>>(`${this.baseUrl}products`, {
      observe: 'response',
      params
    }).pipe(
      map(response => response.body?.data ?? [])
    );
  }

  getShopParams(): ShopParams {
    return this.shopParams();
  }

  setShopParams(params: ShopParams): void {
    this.shopParams.set(params);
  }

  getProduct(id: number): Observable<Product> {
    const product = this.products.find(p => p.id === id);

    if (product) {
      return of(product);
    }

    return this.http.get<Product>(`${this.baseUrl}products/${id}`);
  }

  getBrands(): Observable<Brand[]> {
    if (this.brands.length > 0) {
      return of(this.brands);
    }
    return this.http.get<Brand[]>(`${this.baseUrl}products/brands`).pipe(
      tap(response => {
        this.brands = response;
      })
    );
  }

  getTypes(): Observable<ProductType[]> {
    if (this.types.length > 0) {
      return of(this.types);
    }
    return this.http.get<ProductType[]>(`${this.baseUrl}products/types`).pipe(
      tap(response => {
        this.types = response;
      })
    );
  }
}
