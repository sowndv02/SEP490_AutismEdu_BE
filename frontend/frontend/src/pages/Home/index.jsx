import { Typography } from '@mui/material'
import services from '~/plugins/services'

function Home() {
    const handleGetData = async () => {
        console.log("run test");
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
            <Typography variant='h1'>This is homepage</Typography>
            <button onClick={handleGetData}>
                get data
            </button>
        </>
    )
}

export default Home
