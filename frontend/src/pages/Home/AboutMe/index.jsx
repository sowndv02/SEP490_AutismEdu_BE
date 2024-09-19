<<<<<<< HEAD
import { Box, Button, Chip, Grid, Typography } from '@mui/material';
import teacher from '~/assets/teacher.png';
function AboutMe() {
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center' }}>
            <Grid container mb={5}>
                <Grid item xs={2}></Grid>
                <Grid item xs={8}>
                    <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={12} md={6} >
                            <img src={teacher} />
                        </Grid>
                        <Grid item xs={12} md={6} sx={{ height: "510px" }}>
                            <Chip
                                label={"THÔNG TIN VỀ CHÚNG TÔI"}
                                sx={{
                                    fontSize: '14px',
                                    padding: '20px 10px',
                                    bgcolor: "#f5e7f1",
                                    fontWeight: "bold",
                                    color: "#DB7093"
                                }}
                            />
                            <Typography variant='h3' sx={{ fontSize: "44px", color: "#192335", fontWeight: "bold" }} mt={3}>
                                Thông Tin Về Hệ Thống AutismEdu
                            </Typography>
                            <Typography sx={{
                                mt: "20px",
                                fontSize: "20px"
                            }}>Far far away, behind the word mountains, far from the countries Vokalia and Consonantia, there live the blind texts. Separated they live in Bookmarksgrove right at the coast of the Semantics, a large language ocean.</Typography>

                            <Button>Tìm hiểu thêm</Button>
                        </Grid>
                    </Grid>
                </Grid>
                <Grid item xs={2}></Grid>
            </Grid >
        </Box >
=======
import { Box, Button, Chip, Divider, Grid, Stack, Typography } from '@mui/material';
import teacher from '~/assets/teacher.png';
function AboutMe() {
    return (
        <Stack direction='row' sx={{ width: "100vw", pt: "100px", justifyContent: 'center' }}>
            <Box sx={{
                width: {
                    xl: "80%",
                    lg: "90%"
                },
            }}>
                <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                    <Grid item xs={12} md={6} sx={{
                        backgroundImage: `url(${teacher})`,
                        backgroundSize: "cover",
                        backgroundRepeat: "no-repeat"
                    }}>
                    </Grid>
                    <Grid item xs={12} md={6} sx={{ height: "510px" }}>
                        <Chip
                            label={"THÔNG TIN VỀ CHÚNG TÔI"}
                            sx={{
                                fontSize: '14px',
                                padding: '20px 10px',
                                bgcolor: "#f5e7f1",
                                fontWeight: "bold",
                                color: "#DB7093"
                            }}
                        />
                        <Typography variant='h3' sx={{ fontSize: "44px", color: "#192335", fontWeight: "bold" }} mt={3}>
                            Thông Tin Về Hệ Thống AutismEdu
                        </Typography>
                        <Typography sx={{
                            mt: "20px",
                            fontSize: "20px"
                        }}>Far far away, behind the word mountains, far from the countries Vokalia and Consonantia, there live the blind texts. Separated they live in Bookmarksgrove right at the coast of the Semantics, a large language ocean.</Typography>

                        <Button sx={{ fontSize: "20px", mt: "20px" }}>Tìm hiểu thêm</Button>
                    </Grid>
                </Grid>
                <Divider sx={{mt:"80px"}}/>
            </Box>
        </Stack >
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    )
}
export default AboutMe
