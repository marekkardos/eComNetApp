import { Order } from '../models';

export const MOCK_ORDERS: Order[] = [
  {
    id: 1001,
    buyerEmail: 'test@test.com',
    orderDate: new Date('2026-01-20T10:30:00'),
    shipToAddress: {
      firstName: 'John',
      lastName: 'Doe',
      street: '123 Main Street',
      city: 'New York',
      state: 'NY',
      zipcode: '10001'
    },
    deliveryMethod: 'UPS1',
    shippingPrice: 10,
    orderItems: [
      {
        productId: 1,
        productName: 'Angular Purple Boots',
        pictureUrl: 'https://picsum.photos/seed/boot1/400/400',
        price: 199.99,
        quantity: 1
      },
      {
        productId: 4,
        productName: 'TypeScript Navy Hat',
        pictureUrl: 'https://picsum.photos/seed/hat1/400/400',
        price: 29.99,
        quantity: 2
      }
    ],
    subtotal: 259.97,
    total: 269.97,
    status: 'Delivered'
  },
  {
    id: 1002,
    buyerEmail: 'test@test.com',
    orderDate: new Date('2026-01-25T14:15:00'),
    shipToAddress: {
      firstName: 'John',
      lastName: 'Doe',
      street: '123 Main Street',
      city: 'New York',
      state: 'NY',
      zipcode: '10001'
    },
    deliveryMethod: 'UPS2',
    shippingPrice: 5,
    orderItems: [
      {
        productId: 2,
        productName: 'React Blue Sneakers',
        pictureUrl: 'https://picsum.photos/seed/sneaker1/400/400',
        price: 149.99,
        quantity: 1
      }
    ],
    subtotal: 149.99,
    total: 154.99,
    status: 'Pending'
  },
  {
    id: 1003,
    buyerEmail: 'test@test.com',
    orderDate: new Date('2026-01-26T09:00:00'),
    shipToAddress: {
      firstName: 'John',
      lastName: 'Doe',
      street: '123 Main Street',
      city: 'New York',
      state: 'NY',
      zipcode: '10001'
    },
    deliveryMethod: 'FREE',
    shippingPrice: 0,
    orderItems: [
      {
        productId: 3,
        productName: 'Vue Green Sandals',
        pictureUrl: 'https://picsum.photos/seed/sandal1/400/400',
        price: 79.99,
        quantity: 2
      },
      {
        productId: 7,
        productName: 'Vue Orange Hat',
        pictureUrl: 'https://picsum.photos/seed/hat2/400/400',
        price: 24.99,
        quantity: 1
      }
    ],
    subtotal: 184.97,
    total: 184.97,
    status: 'Payment Received'
  }
];
