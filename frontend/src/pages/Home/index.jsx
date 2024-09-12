import { Typography } from '@mui/material'
import services from '~/plugins/services'

function Home() {
    const handleGetData = async () => {
        await services.AuthenticationAPI.getData(
            (res) => {
                console.log(res);
            },
            (err) => {
                console.log(err);
            }, {}
        )
    }
    return (
        <>
            <Typography variant='h1' sx={{ color: "secondary.main" }}>This is homepage</Typography>
            <Typography>{import.meta.env.VITE_BASE_URL}</Typography>
            <button onClick={handleGetData}>
                get data
            </button>
        </>
    )
}

export default Home
