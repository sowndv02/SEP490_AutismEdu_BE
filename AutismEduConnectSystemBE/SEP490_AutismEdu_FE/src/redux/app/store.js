import { configureStore } from '@reduxjs/toolkit'
import userSlice from '../features/userSlice'
import tutorSlice from '../features/tutorSlice'
import listStudentSlice from '../features/listStudent'
import adminSlice from '../features/adminSlice'
import packagePaymentSlice from '../features/packagePaymentSlice'

export default configureStore({
  reducer: {
    user: userSlice,
    tutor: tutorSlice,
    listStudent: listStudentSlice,
    admin: adminSlice,
    pkgPayment: packagePaymentSlice
  }
})