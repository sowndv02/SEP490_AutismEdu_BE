import { createSlice } from '@reduxjs/toolkit'
export const packagePaymentSlice = createSlice({
    name: 'pkgPayment',
    initialState: {
        value: null
    },
    reducers: {
        setPackagePayment: (state, action) => {
            state.value = action.payload;
        }
    }
})

export const { setPackagePayment } = packagePaymentSlice.actions

export const packagePayment = (state) => state.pkgPayment.value
export default packagePaymentSlice.reducer