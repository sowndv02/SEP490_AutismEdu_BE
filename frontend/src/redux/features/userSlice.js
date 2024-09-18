import { createSlice } from '@reduxjs/toolkit'
export const userSlice = createSlice({
    name: 'user',
    initialState: {
        value: null
    },
    reducers: {
        setUserInformation: (state, action) => {
            state.value = action.payload;
        }
    }
})

export const { setUserInformation } = userSlice.actions

export const userInfor = (state) => state.user.value
export default userSlice.reducer