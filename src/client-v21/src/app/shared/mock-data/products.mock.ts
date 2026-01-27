import { Product, Brand, ProductType } from '../models';

export const MOCK_PRODUCTS: Product[] = [
  {
    id: 1,
    name: 'Angular Purple Boots',
    description: 'Premium leather boots with Angular logo. Perfect for developers who want to walk with confidence.',
    price: 199.99,
    pictureUrl: 'https://picsum.photos/seed/boot1/400/400',
    productType: 'Boots',
    productBrand: 'Angular'
  },
  {
    id: 2,
    name: 'React Blue Sneakers',
    description: 'Comfortable sneakers for the modern React developer. Lightweight and stylish.',
    price: 149.99,
    pictureUrl: 'https://picsum.photos/seed/sneaker1/400/400',
    productType: 'Sneakers',
    productBrand: 'React'
  },
  {
    id: 3,
    name: 'Vue Green Sandals',
    description: 'Eco-friendly sandals for Vue enthusiasts. Made with sustainable materials.',
    price: 79.99,
    pictureUrl: 'https://picsum.photos/seed/sandal1/400/400',
    productType: 'Sandals',
    productBrand: 'Vue'
  },
  {
    id: 4,
    name: 'TypeScript Navy Hat',
    description: 'Classic cap with TypeScript branding. One size fits all.',
    price: 29.99,
    pictureUrl: 'https://picsum.photos/seed/hat1/400/400',
    productType: 'Hats',
    productBrand: 'TypeScript'
  },
  {
    id: 5,
    name: 'Angular Red Sneakers',
    description: 'Bold red sneakers with Angular branding. Stand out from the crowd.',
    price: 169.99,
    pictureUrl: 'https://picsum.photos/seed/sneaker2/400/400',
    productType: 'Sneakers',
    productBrand: 'Angular'
  },
  {
    id: 6,
    name: 'React Black Boots',
    description: 'Sleek black boots for React developers. Durable and waterproof.',
    price: 219.99,
    pictureUrl: 'https://picsum.photos/seed/boot2/400/400',
    productType: 'Boots',
    productBrand: 'React'
  },
  {
    id: 7,
    name: 'Vue Orange Hat',
    description: 'Bright orange cap with Vue.js logo. Perfect for sunny days.',
    price: 24.99,
    pictureUrl: 'https://picsum.photos/seed/hat2/400/400',
    productType: 'Hats',
    productBrand: 'Vue'
  },
  {
    id: 8,
    name: 'TypeScript White Sneakers',
    description: 'Clean white sneakers with TypeScript logo. Minimalist design.',
    price: 139.99,
    pictureUrl: 'https://picsum.photos/seed/sneaker3/400/400',
    productType: 'Sneakers',
    productBrand: 'TypeScript'
  },
  {
    id: 9,
    name: 'Angular Teal Sandals',
    description: 'Comfortable sandals in Angular teal color. Great for summer.',
    price: 69.99,
    pictureUrl: 'https://picsum.photos/seed/sandal2/400/400',
    productType: 'Sandals',
    productBrand: 'Angular'
  },
  {
    id: 10,
    name: 'React Premium Boots',
    description: 'Premium quality boots with React branding. Built to last.',
    price: 249.99,
    pictureUrl: 'https://picsum.photos/seed/boot3/400/400',
    productType: 'Boots',
    productBrand: 'React'
  },
  {
    id: 11,
    name: 'Vue Classic Sneakers',
    description: 'Classic style sneakers for Vue developers. Timeless design.',
    price: 129.99,
    pictureUrl: 'https://picsum.photos/seed/sneaker4/400/400',
    productType: 'Sneakers',
    productBrand: 'Vue'
  },
  {
    id: 12,
    name: 'TypeScript Blue Hat',
    description: 'Stylish blue cap with TypeScript logo. Adjustable strap.',
    price: 34.99,
    pictureUrl: 'https://picsum.photos/seed/hat3/400/400',
    productType: 'Hats',
    productBrand: 'TypeScript'
  }
];

export const MOCK_BRANDS: Brand[] = [
  { id: 1, name: 'Angular' },
  { id: 2, name: 'React' },
  { id: 3, name: 'Vue' },
  { id: 4, name: 'TypeScript' }
];

export const MOCK_TYPES: ProductType[] = [
  { id: 1, name: 'Boots' },
  { id: 2, name: 'Sneakers' },
  { id: 3, name: 'Sandals' },
  { id: 4, name: 'Hats' }
];
