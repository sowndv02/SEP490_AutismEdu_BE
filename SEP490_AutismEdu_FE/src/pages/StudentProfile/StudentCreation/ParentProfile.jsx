import { Card, CardContent, FormControl, FormHelperText, Grid, MenuItem, Select, TextField, Typography } from '@mui/material';
import axios from 'axios';
import { useEffect } from 'react';
function ParentProfile({ parent, hasAccount, formik,
    provinces, setProvinces, districts, setDistricts, communes, setCommunes
}) {
    useEffect(() => {
        handleGetProvince();
    }, [])
    const fomatAddress = (address) => {
        const addressArr = address.split("|");
        return `${addressArr[3]} - ${addressArr[2]} - ${addressArr[1]} - ${addressArr[0]}`
    }

    const handleGetProvince = async () => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/province")
            setProvinces(data.data)
        } catch (error) {
            console.log(error);
        }
    }

    const handleGetDistrict = async (id) => {
        try {
            if (id?.length !== 0) {
                const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/district?idProvince=" + id);
                setDistricts(data.data);
                return data.data
            }
        } catch (error) {
            console.log(error);
        }
    }
    const handleGetCommunes = async (id) => {
        try {
            const data = await axios.get("https://vietnam-administrative-division-json-server-swart.vercel.app/commune?idDistrict=" + id);
            setCommunes(data.data);
            return data.data
        } catch (error) {
            console.log(error);
        }
    }

    return (
        <>
            <Card sx={{ px: 2 }}>
                <CardContent sx={{ px: 0 }}>
                    <Typography variant='h5'>Thông tin phụ huynh</Typography>
                    {
                        parent && hasAccount === "true" && (
                            <>
                                <Grid container rowSpacing={2} mt={1}>
                                    <Grid item xs={4}>
                                        <Typography>Họ tên:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography>{parent.fullName}</Typography>
                                    </Grid>
                                    <Grid item xs={4}>
                                        <Typography>Số điện thoại:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography>{parent.phoneNumber}</Typography>
                                    </Grid>
                                    <Grid item xs={4}>
                                        <Typography>Địa chỉ:</Typography>
                                    </Grid>
                                    <Grid item xs={8}>
                                        <Typography>{fomatAddress(parent.address)}</Typography>
                                    </Grid>
                                </Grid>
                            </>
                        )
                    }
                    {
                        hasAccount === "false" && (
                            <>
                                <CardContent sx={{ p: 0 }}>
                                    <Grid container rowSpacing={2} mt={1}>
                                        <Grid item xs={4}>
                                            <Typography>Họ tên:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <TextField size='small' fullWidth
                                                name='parentName'
                                                value={formik.values.parentName}
                                                onChange={formik.handleChange}
                                            />
                                            {
                                                formik.errors.parentName && (
                                                    <FormHelperText error>
                                                        {formik.errors.parentName}
                                                    </FormHelperText>
                                                )
                                            }
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Email:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <TextField size='small' fullWidth
                                                name='email'
                                                value={formik.values.email}
                                                onChange={formik.handleChange}
                                            />
                                            {
                                                formik.errors.email && (
                                                    <FormHelperText error>
                                                        {formik.errors.email}
                                                    </FormHelperText>
                                                )
                                            }
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Số điện thoại:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <TextField size='small' fullWidth
                                                name='phoneNumber'
                                                value={formik.values.phoneNumber}
                                                onChange={formik.handleChange}
                                            />
                                            {
                                                formik.errors.phoneNumber && (
                                                    <FormHelperText error>
                                                        {formik.errors.phoneNumber}
                                                    </FormHelperText>
                                                )
                                            }
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Tỉnh / Thành phố:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <FormControl fullWidth>
                                                <Select
                                                    labelId="province"
                                                    value={formik.values.province}
                                                    onChange={(event) => {
                                                        const selectedProvince = event.target.value;
                                                        if (selectedProvince && formik.values.province !== selectedProvince) {
                                                            formik.handleChange(event);
                                                            handleGetDistrict(event.target.value);
                                                            setCommunes([]);
                                                            formik.setFieldValue('district', '')
                                                            formik.setFieldValue('commune', '')
                                                        }
                                                    }}
                                                    name='province'
                                                    renderValue={(selected) => {
                                                        if (!selected || selected === "") {
                                                            return <em>Tỉnh / TP</em>;
                                                        }
                                                        const selectedProvince = provinces.find(p => p.idProvince === selected);
                                                        return selectedProvince ? selectedProvince.name : "";
                                                    }}
                                                >
                                                    {
                                                        provinces.length !== 0 && provinces?.map((province) => {
                                                            return (
                                                                <MenuItem value={province?.idProvince} key={province?.idProvince}>{province.name}</MenuItem>
                                                            )
                                                        })
                                                    }
                                                </Select>
                                                {
                                                    formik.errors.province && (
                                                        <FormHelperText error>
                                                            {formik.errors.province}
                                                        </FormHelperText>
                                                    )
                                                }
                                            </FormControl>
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Huyện / Quận:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <FormControl fullWidth>
                                                <Select
                                                    labelId="district"
                                                    value={formik.values.district}
                                                    name='district'
                                                    onChange={(event) => {
                                                        formik.handleChange(event); handleGetCommunes(event.target.value);
                                                        formik.setFieldValue('commune', '')
                                                    }}
                                                    renderValue={(selected) => {
                                                        if (!selected || selected === "") {
                                                            return <em>Quận / Huyện</em>;
                                                        }
                                                        const selectedDistrict = districts.find(p => p.idDistrict === selected);
                                                        return selectedDistrict ? selectedDistrict.name : <em>Quận / Huyện</em>;
                                                    }}
                                                    disabled={districts.length === 0}
                                                >
                                                    {
                                                        districts.length !== 0 && districts?.map((district) => {
                                                            return (
                                                                <MenuItem value={district?.idDistrict} key={district?.idDistrict}>{district.name}</MenuItem>
                                                            )
                                                        })
                                                    }
                                                </Select>
                                                {
                                                    formik.errors.district && (
                                                        <FormHelperText error>
                                                            {formik.errors.district}
                                                        </FormHelperText>
                                                    )
                                                }
                                            </FormControl>
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Xã / Phường:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <FormControl fullWidth>
                                                <Select
                                                    labelId="commune"
                                                    value={formik.values.commune}
                                                    name='commune'
                                                    onChange={formik.handleChange}
                                                    renderValue={(selected) => {
                                                        if (!selected || selected === "") {
                                                            return <em>Xã / Phường</em>;
                                                        }
                                                        const selectedCommune = communes.find(p => p.idCommune === selected);
                                                        return selectedCommune ? selectedCommune.name : <em>Xã / Phường</em>;
                                                    }}
                                                    disabled={communes.length === 0}
                                                >
                                                    {
                                                        communes.length !== 0 && communes?.map((commune) => {
                                                            return (
                                                                <MenuItem value={commune?.idCommune} key={commune?.idCommune}>{commune.name}</MenuItem>
                                                            )
                                                        })
                                                    }
                                                </Select>
                                                {
                                                    formik.errors.commune && (
                                                        <FormHelperText error>
                                                            {formik.errors.commune}
                                                        </FormHelperText>
                                                    )
                                                }
                                            </FormControl>
                                        </Grid>
                                        <Grid item xs={4}>
                                            <Typography>Số nhà:</Typography>
                                        </Grid>
                                        <Grid item xs={8}>
                                            <TextField size='small' fullWidth
                                                name='homeNumber'
                                                value={formik.values.homeNumber}
                                                onChange={formik.handleChange}
                                            />
                                            {
                                                formik.errors.homeNumber && (
                                                    <FormHelperText error>
                                                        {formik.errors.homeNumber}
                                                    </FormHelperText>
                                                )
                                            }
                                        </Grid>
                                    </Grid>
                                </CardContent>
                            </>
                        )
                    }
                </CardContent>
            </Card>
        </>
    )
}

export default ParentProfile
