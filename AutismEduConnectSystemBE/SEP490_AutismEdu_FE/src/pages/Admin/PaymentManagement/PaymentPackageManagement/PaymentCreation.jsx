import * as React from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Modal from '@mui/material/Modal';
import { useFormik } from 'formik';
import { FormControl, FormHelperText, MenuItem, Select, Stack, TextField } from '@mui/material';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import PropTypes from 'prop-types';
import { NumericFormat } from 'react-number-format';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 700,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4
};

const NumericFormatCustom = React.forwardRef(
    function NumericFormatCustom(props, ref) {
        const { onChange, ...other } = props;

        return (
            <NumericFormat
                {...other}
                getInputRef={ref}
                onValueChange={(values) => {
                    onChange({
                        target: {
                            name: props.name,
                            value: values.value
                        }
                    });
                }}
                isAllowed={(values) => {
                    const { floatValue } = values;
                    return floatValue === undefined || floatValue >= 0;
                }}
                thousandSeparator="."
                decimalSeparator=","
                valueIsNumericString
            />
        );
    }
);

NumericFormatCustom.propTypes = {
    name: PropTypes.string.isRequired,
    onChange: PropTypes.func.isRequired
};
export default function PaymentCreation({ change, setChange }) {
    const [open, setOpen] = React.useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [value, setValue] = React.useState({
        price: ""
    });
    const validate = values => {
        const errors = {}
        if (!values.title) {
            errors.title = "Bắt buộc";
        } else if (values.title.length > 100) {
            errors.title = "Tên chỉ chứa tối đa 100 kí tự";
        }
        if (values.description.length > 500) {
            errors.description = "Phải dưới 500 ký tự"
        }
        if (!values.duration) {
            errors.duration = "Bắt buộc";
        }
        if (!values.price) {
            errors.price = "Bắt buộc";
        } else if (Number(values.price) < 10000) {
            errors.price = "Giá tối thiểu là 10.000 vnđ"
        } else if (Number(values.price) > 1000000000) {
            errors.price = "Giá tối đa là 1.000.000.000 vnđ"
        }
        return errors
    }
    const formik = useFormik({
        initialValues: {
            title: '',
            duration: '',
            description: '',
            price: '',
            isHide: true,
            originalId: 0
        }, validate,
        onSubmit: async (values) => {
            try {
                await services.PackagePaymentAPI.createPaymentPackage(values, (res) => {
                    setChange(!change);
                    enqueueSnackbar("Tạo gói thành toán thành công!", { variant: "success" });
                    formik.resetForm();
                    handleClose();
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                })
            } catch (error) {
                enqueueSnackbar("Tạo tạo gói thanh toán thất bại", { variant: "error" })
            }
        }
    })

    const handleChange = (event) => {
        if (event.target.value[0] === "-") {
            formik.setFieldValue(event.target.name, "0")
        } else {
            formik.setFieldValue(event.target.name, event.target.value)
        }
        setValue({
            ...value,
            [event.target.name]: event.target.value
        });
    };
    return (
        <>
            <Button onClick={handleOpen} variant='contained'>Tạo gói thanh toán mới</Button>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={style}>
                    <Typography textAlign={'center'} variant="h4" mb={3}>
                        Tạo gói thanh toán mới
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Typography>Tên gói</Typography>
                        <TextField
                            fullWidth
                            name='title'
                            value={formik.values.title}
                            onChange={formik.handleChange}
                        />
                        {
                            formik.errors.title && (
                                <FormHelperText error>
                                    {formik.errors.title}
                                </FormHelperText>
                            )
                        }
                        <Typography mt={2}>Mô tả</Typography>
                        <TextField
                            multiline
                            rows={5}
                            fullWidth
                            name='description'
                            value={formik.values.description}
                            onChange={formik.handleChange}
                        />
                        <Typography sx={{ fontSize: '12px', textAlign: "right" }}>
                            {formik.values.description.length} / 500
                        </Typography>
                        {
                            formik.errors.description && (
                                <FormHelperText error>
                                    {formik.errors.description}
                                </FormHelperText>
                            )
                        }
                        <Stack direction='row' mt={2}>
                            <Box sx={{ width: "50%" }}>
                                <Typography>Khoảng thời gian (tháng): </Typography>
                                <TextField
                                    name='duration'
                                    onChange={formik.handleChange}
                                    value={formik.values.duration}
                                    type='Number'
                                    inputProps={{
                                        min: 1,
                                        max: 1200
                                    }}
                                />
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                <Typography>Giá (VND)</Typography>
                                <TextField
                                    variant="outlined"
                                    name="price"
                                    value={formik.values.price}
                                    onChange={handleChange}
                                    InputProps={{
                                        inputComponent: NumericFormatCustom
                                    }}
                                />
                            </Box>
                        </Stack>
                        <Stack direction='row'>
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.duration && (
                                        <FormHelperText error>
                                            {formik.errors.duration}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                {
                                    formik.errors.price && (
                                        <FormHelperText error>
                                            {formik.errors.price}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                        </Stack>
                        <Typography>Bạn có muốn hiển thị gói này luôn không?</Typography>
                        <FormControl sx={{ width: "100px" }}>
                            <Select name='isHide' value={formik.values.isHide} onChange={formik.handleChange} >
                                <MenuItem value={false}>Có</MenuItem>
                                <MenuItem value={true}>Không</MenuItem>
                            </Select>
                        </FormControl>
                        <Box>
                            <Button variant='contained' type='submit' sx={{ mt: 2 }}>Tạo gói thanh toán</Button>
                        </Box>
                    </form>
                </Box>
            </Modal>
        </>
    );
}
