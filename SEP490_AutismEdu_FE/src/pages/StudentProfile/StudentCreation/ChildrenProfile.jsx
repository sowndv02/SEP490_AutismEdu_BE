import { Box, Card, CardContent, CardMedia, FormControl, FormHelperText, Grid, MenuItem, Select, TextField, Typography } from '@mui/material';
import ModalUploadAvatar from '~/pages/Tutor/TutorRegistration/TutorInformation/ModalUploadAvatar';
function ChildrenProfile({ childrenInfo, currentChild, hasAccount, formik, setAvatar, avatar }) {
    const getMaxDate = () => {
        const today = new Date();
        const lastYear = new Date(today);
        lastYear.setFullYear(today.getFullYear() - 1);
        const year = lastYear.getFullYear();
        const month = String(lastYear.getMonth() + 1).padStart(2, '0');
        const day = String(lastYear.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    const getMinDate = () => {
        const today = new Date();
        const fifteenYearsAgo = new Date(today);
        fifteenYearsAgo.setFullYear(today.getFullYear() - 15, 0, 1);
        const year = fifteenYearsAgo.getFullYear();
        const month = String(fifteenYearsAgo.getMonth() + 1).padStart(2, '0');
        const day = String(fifteenYearsAgo.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    return (
        <Card sx={{ mt: 3, px: 2 }}>
            <CardContent sx={{ px: 0 }}>
                <Typography variant='h5'>Thông tin trẻ</Typography>
            </CardContent>
            {
                (childrenInfo.length !== 0 && hasAccount === "true") && (
                    <>
                        <CardMedia
                            sx={{ height: "250px" }}
                            image={childrenInfo[currentChild].imageUrlPath}
                            title="green iguana"
                        />
                        <CardContent sx={{ p: 0 }}>
                            <Grid container rowSpacing={2} mt={1}>
                                <Grid item xs={4}>
                                    <Typography>Họ tên trẻ:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <Typography>{childrenInfo[currentChild].name}</Typography>
                                </Grid>
                                <Grid item xs={4}>
                                    <Typography>Ngày sinh:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <Typography>{childrenInfo[currentChild].birthDate.split('T')[0]}</Typography>
                                </Grid>
                                <Grid item xs={4}>
                                    <Typography>Giới tính:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <Typography>{childrenInfo[currentChild].gender === "Male" ? "Nam" : "Nữ"}</Typography>
                                </Grid>
                            </Grid>
                        </CardContent>
                    </>
                )
            }
            {
                hasAccount === "false" && (
                    <>
                        <CardContent sx={{ p: 0 }}>
                            <Grid container rowSpacing={2} mt={1}>
                                <Grid item xs={4}>
                                    <Typography>Ảnh của trẻ:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <ModalUploadAvatar setAvatar={setAvatar} />
                                    {
                                        !avatar && <FormHelperText error>
                                            Bắt buộc
                                        </FormHelperText>
                                    }
                                    <Box>
                                        {
                                            avatar &&
                                            <img src={URL.createObjectURL(avatar)} alt='avatar' width={150} />
                                        }
                                    </Box>
                                </Grid>
                                <Grid item xs={4}>
                                    <Typography>Họ tên:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <TextField size='small' fullWidth
                                        value={formik.values.childName}
                                        onChange={formik.handleChange}
                                        name='childName'
                                    />
                                    {
                                        formik.errors.childName && (
                                            <FormHelperText error>
                                                {formik.errors.childName}
                                            </FormHelperText>
                                        )
                                    }
                                </Grid>
                                <Grid item xs={4}>
                                    <Typography>Ngày sinh:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <TextField size='small' type='date' value={formik.values.dateOfBirth}
                                        name='dateOfBirth'
                                        onChange={formik.handleChange}
                                        inputProps={{
                                            max: getMaxDate(),
                                            min: getMinDate()
                                        }} />
                                    {
                                        formik.errors.dateOfBirth && (
                                            <FormHelperText error>
                                                {formik.errors.dateOfBirth}
                                            </FormHelperText>
                                        )
                                    }
                                </Grid>
                                <Grid item xs={4}>
                                    <Typography>Giới tính:</Typography>
                                </Grid>
                                <Grid item xs={8}>
                                    <FormControl fullWidth size='small'>
                                        <Select
                                            name='gender'
                                            value={formik.values.gender}
                                            onChange={formik.handleChange}
                                            fullWidth
                                        >
                                            <MenuItem value={"True"}>Nam</MenuItem>
                                            <MenuItem value={"False"}>Nữ</MenuItem>
                                        </Select>
                                    </FormControl>
                                    {
                                        formik.errors.gender && (
                                            <FormHelperText error>
                                                {formik.errors.gender}
                                            </FormHelperText>
                                        )
                                    }
                                </Grid>
                            </Grid>
                        </CardContent>
                    </>
                )
            }
        </Card>
    )
}

export default ChildrenProfile
